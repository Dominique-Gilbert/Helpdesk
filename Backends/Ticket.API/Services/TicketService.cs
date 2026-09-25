using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Helpdesk.Contracts.Events;
using Helpdesk.Mapping;
using Helpdesk.Messaging;
using Helpdesk.ServiceDefaults;
using Helpdesk.Tickets.Domain.Model;
using Helpdesk.Tickets.Grpc;
using Helpdesk.Tickets.Infrastructure;
using Helpdesk.Tickets.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Tickets.Api.Services;

public class TicketService(
    TicketContext db,
    TicketRepo repo,
    IMapper mapper,
    IEventPublisher publisher,
    ILogger<TicketService> logger) : TicketGrpc.TicketGrpcBase
{
    public override async Task<TicketResponse> CreateTicket(CreateTicketRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title is required."));
        }
        ValidateLength(request.Title, TitleMaxLength, nameof(request.Title));
        ValidateLength(request.Description, DescriptionMaxLength, nameof(request.Description));
        ValidateLength(request.RequestedBy, RequestedByMaxLength, nameof(request.RequestedBy));
        ValidateLength(request.Comment, CommentBodyMaxLength, nameof(request.Comment));

        var caller = CurrentUser(context);

        var ticket = new Ticket
        {
            Reference = BuildReference(),
            Title = request.Title.Trim(),
            Description = request.Description ?? string.Empty,
            RequestedBy = request.RequestedBy ?? string.Empty,
            RequestedByUserId = caller.Id,
            RequestedByUsername = caller.Username,
            RequestedByRole = caller.Role,
            ClientId = caller.ClientId,
            Category = ProtoConverters.ToEnum(request.Category, TicketCategory.General),
            Priority = ProtoConverters.ToEnum(request.Priority, TicketPriority.Normal),
            Status = TicketStatus.New,
            CreatedAtUtc = DateTime.UtcNow
        };

        var comment = request.Comment?.Trim() ?? string.Empty;
        if (comment.Length > 0)
        {
            db.Comments.Add(new Comment
            {
                TicketId = ticket.Id,
                Author = string.IsNullOrWhiteSpace(caller.Username) ? ticket.RequestedBy : caller.Username,
                Body = comment,
                CreatedAtUtc = ticket.CreatedAtUtc
            });
        }

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(context.CancellationToken);

        // Commit first, then announce. No addressed recipient - Assignment.API and SLA.API
        // each decide for themselves whether they care.
        // Known gap: not transactional with the save. See README "Known gaps".
        await publisher.PublishAsync(new TicketCreated
        {
            TicketId = ticket.Id,
            Reference = ticket.Reference,
            Title = ticket.Title,
            Category = ticket.Category.ToString(),
            Priority = ticket.Priority.ToString(),
            RequestedBy = ticket.RequestedBy,
            Comment = comment,
            CreatedAtUtc = ticket.CreatedAtUtc,
            ClientId = ticket.ClientId
        }, context.CancellationToken);

        logger.LogInformation("Created ticket {Reference} ({TicketId}) and published Ticket.Created",
            ticket.Reference, ticket.Id);

        return ToResponse(ticket);
    }

    public override async Task<TicketResponse> GetTicket(TicketRequest request, ServerCallContext context)
    {
        var ticket = await repo.GetWithCommentsAsync(ParseId(request.Id), context.CancellationToken)
            ?? throw NotFound(request.Id);

        return ToResponse(ticket);
    }

    public override async Task<TicketListResponse> ListTickets(ListTicketsRequest request, ServerCallContext context)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

        TicketStatus? status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : ProtoConverters.ToEnum(request.Status, TicketStatus.New);
        TicketPriority? priority = string.IsNullOrWhiteSpace(request.Priority)
            ? null
            : ProtoConverters.ToEnum(request.Priority, TicketPriority.Normal);

        var tickets = await repo.ListAsync(status, priority, (page - 1) * pageSize, pageSize, context.CancellationToken);
        var total = await repo.CountAsync(status, priority, context.CancellationToken);

        var response = new TicketListResponse { Total = total, Page = page, PageSize = pageSize };
        response.Tickets.AddRange(tickets.Select(ToResponse));
        return response;
    }

    public override async Task<TicketResponse> UpdateTicket(UpdateTicketRequest request, ServerCallContext context)
    {
        var ticket = await repo.GetWithCommentsAsync(ParseId(request.Id), context.CancellationToken)
            ?? throw NotFound(request.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            ValidateLength(request.Title, TitleMaxLength, nameof(request.Title));
            ticket.Title = request.Title.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            ValidateLength(request.Description, DescriptionMaxLength, nameof(request.Description));
            ticket.Description = request.Description;
        }
        // Status is deliberately NOT settable here, even though the field exists on the wire
        // contract - CloseTicket (ClosedAtUtc + Ticket.Closed) and a comment landing inside the
        // grace window (Ticket.Reopened) are the only two valid state transitions, because both
        // carry the side effects that keep SLA.API and Assignment.API in sync. A blind status
        // write here would flip Status to Closed without ever publishing Ticket.Closed - the SLA
        // clock keeps running and the technician's load slot never frees, even though every list
        // screen reads the ticket as done.
        if (!string.IsNullOrWhiteSpace(request.Priority)) ticket.Priority = ProtoConverters.ToEnum(request.Priority, ticket.Priority);

        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        return ToResponse(ticket);
    }

    public override async Task<TicketResponse> CloseTicket(TicketRequest request, ServerCallContext context)
    {
        var ticket = await repo.GetWithCommentsAsync(ParseId(request.Id), context.CancellationToken)
            ?? throw NotFound(request.Id);

        // Idempotent: closing an already-closed/completed ticket is a no-op rather than
        // re-publishing Ticket.Closed and re-stamping ClosedAtUtc, which would reset its
        // 24h grace window every time someone double-clicks "Close".
        if (ticket.Status is TicketStatus.Closed or TicketStatus.Completed)
        {
            return ToResponse(ticket);
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = ticket.ClosedAtUtc;
        await db.SaveChangesAsync(context.CancellationToken);

        // SLA.API owns the clock; it stops its own on the strength of the event alone,
        // exactly like the fan-out on the way in - no direct call, no knowledge of SLA.API.
        await publisher.PublishAsync(new TicketClosed
        {
            TicketId = ticket.Id,
            Reference = ticket.Reference,
            ClosedAtUtc = ticket.ClosedAtUtc.Value,
            ClientId = ticket.ClientId
        }, context.CancellationToken);

        logger.LogInformation("Closed ticket {Reference} and published Ticket.Closed", ticket.Reference);

        return ToResponse(ticket);
    }

    public override async Task<TicketResponse> AddComment(AddCommentRequest request, ServerCallContext context)
    {
        var ticketId = ParseId(request.TicketId);
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, context.CancellationToken)
            ?? throw NotFound(request.TicketId);

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Comment body is required."));
        }
        ValidateLength(request.Body, CommentBodyMaxLength, nameof(request.Body));
        ValidateLength(request.Author, CommentAuthorMaxLength, nameof(request.Author));

        db.Comments.Add(new Comment
        {
            TicketId = ticketId,
            Author = string.IsNullOrWhiteSpace(request.Author) ? "unknown" : request.Author,
            Body = request.Body,
            IsInternal = request.IsInternal,
            CreatedAtUtc = DateTime.UtcNow
        });

        // A comment during the 24h post-close grace window means someone still cares about
        // this ticket - bring it back and give it a fresh SLA clock. Past that window the
        // background monitor has already moved it to Completed, which is terminal: a comment
        // on a Completed ticket is just a comment, on the record, nothing reopens.
        var reopened = ticket.Status == TicketStatus.Closed;
        if (reopened)
        {
            ticket.Status = TicketStatus.Reopened;
            ticket.ClosedAtUtc = null;
        }

        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        if (reopened)
        {
            await publisher.PublishAsync(new TicketReopened
            {
                TicketId = ticket.Id,
                Reference = ticket.Reference,
                Priority = ticket.Priority.ToString(),
                ReopenedAtUtc = ticket.UpdatedAtUtc.Value,
                ClientId = ticket.ClientId
            }, context.CancellationToken);

            logger.LogInformation("Reopened ticket {Reference} after a new comment and published Ticket.Reopened",
                ticket.Reference);
        }

        var withComments = await repo.GetWithCommentsAsync(ticketId, context.CancellationToken)
            ?? throw NotFound(request.TicketId);

        return ToResponse(withComments);
    }

    private TicketResponse ToResponse(Ticket ticket)
    {
        var response = mapper.Map<TicketResponse>(ticket);
        response.Comments.AddRange(
            ticket.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(mapper.Map<CommentMessage>));
        return response;
    }

    private readonly record struct CallerInfo(Guid? Id, string Username, string Role, Guid? ClientId);

    /// <summary>
    /// The ticket's owner/creator details for "My Tickets" filtering and the "created by" line
    /// on the ticket - all taken from the caller's own validated JWT (forwarded by the Gateway
    /// as gRPC call credentials), never from client-supplied input. The id is checked under both
    /// claim types since JWT bearer's inbound claim mapping (sub -&gt; NameIdentifier) isn't
    /// something to depend on blindly.
    /// </summary>
    private static CallerInfo CurrentUser(ServerCallContext context)
    {
        var user = context.GetHttpContext()?.User;
        var rawId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return new CallerInfo(
            ProtoConverters.ToNullableGuid(rawId),
            user?.FindFirst(HelpdeskClaimTypes.Username)?.Value ?? string.Empty,
            user?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
            ProtoConverters.ToNullableGuid(user?.FindFirst(HelpdeskClaimTypes.ClientId)?.Value));
    }

    private static string BuildReference() =>
        $"TCK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    // Mirrors TicketContext's HasMaxLength calls - checked here too so an oversized field fails
    // with a clean 400 instead of reaching SaveChangesAsync and throwing an unhandled
    // DbUpdateException, which the Gateway can only surface as a bare 500.
    private const int TitleMaxLength = 256;
    private const int DescriptionMaxLength = 4000;
    private const int RequestedByMaxLength = 128;
    private const int CommentBodyMaxLength = 4000;
    private const int CommentAuthorMaxLength = 128;

    private static void ValidateLength(string? value, int maxLength, string fieldName)
    {
        if (value is { Length: var length } && length > maxLength)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"{fieldName} must not exceed {maxLength} characters (was {length})."));
        }
    }

    private static Guid ParseId(string raw) =>
        Guid.TryParse(raw, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{raw}' is not a valid ticket id."));

    private static RpcException NotFound(string id) =>
        new(new Status(StatusCode.NotFound, $"Ticket '{id}' was not found."));
}
