using AutoMapper;
using Grpc.Core;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Helpdesk.Tickets.Grpc;

namespace Helpdesk.Gateway.Interfacing.Services;

/// <summary>
/// Ticket.API has no idea who's assigned (that's Assignment.API's data) or any concept of
/// roles, so the "who can see/touch this ticket" rule lives here rather than there - the one
/// place that already talks to both. See TicketAccessScope for the rule itself.
/// </summary>
public class TicketService(TicketGrpc.TicketGrpcClient client, IAssignmentService assignments, IMapper mapper) : ITicketService
{
    public async Task<DtoTicket> CreateAsync(CreateTicketDto request, CancellationToken ct = default)
    {
        var response = await client.CreateTicketAsync(new CreateTicketRequest
        {
            Title = request.title ?? string.Empty,
            Description = request.description ?? string.Empty,
            Category = request.category ?? "General",
            Priority = request.priority ?? "Normal",
            RequestedBy = request.requestedBy ?? string.Empty,
            Comment = request.comment ?? string.Empty
        }, cancellationToken: ct);

        return Map(response);
    }

    public async Task<DtoTicket?> GetAsync(Guid id, TicketAccessScope scope, CancellationToken ct = default)
    {
        var ticket = await NotFoundAsNull(async () =>
            Map(await client.GetTicketAsync(new TicketRequest { Id = id.ToString() }, cancellationToken: ct)));

        if (ticket is null) return null;

        // Not found, not forbidden - a ticket that isn't yours shouldn't even confirm it exists.
        return await CanSeeAsync(ticket, scope, ct) ? FilterInternalComments(ticket, scope) : null;
    }

    public async Task<DtoTicketPage> ListAsync(string? status, string? priority, int page, int pageSize, TicketAccessScope scope, CancellationToken ct = default)
    {
        var response = await client.ListTicketsAsync(new ListTicketsRequest
        {
            Status = status ?? string.Empty,
            Priority = priority ?? string.Empty,
            Page = page,
            PageSize = pageSize
        }, cancellationToken: ct);

        var items = response.Tickets.Select(Map).ToList();

        // Post-filtered, not pushed down into the gRPC request - Ticket.API's own paging is
        // over the unfiltered set, so a Technician/Support caller can in principle see fewer
        // than pageSize items even though more of theirs exist further down. Acceptable at this
        // scale (a technician's assigned load is bounded by MaxConcurrent, single digits); doing
        // this properly would mean Ticket.API taking on an opinion about assignment or requester
        // identity, which is exactly the cross-service coupling this split was meant to avoid.
        //
        // BaseRole is the only role not narrowed to a single Client by default - ClientFilter
        // (the profile-menu switcher) optionally narrows it to one specific Client's tickets;
        // everyone else only ever sees their own Client's, on top of their existing role rule.
        if (scope.Role == "BaseRole")
        {
            if (scope.ClientFilter is { } filter) items = items.Where(t => t.clientId == filter).ToList();
        }
        else
        {
            items = items.Where(t => t.clientId == scope.ClientId).ToList();

            items = scope.Role switch
            {
                "Admin" => items,
                "Support" => items.Where(t => t.requestedByUserId == scope.CallerId).ToList(),
                "Technician" when scope.TechnicianId is { } technicianId => await FilterToAssignedAsync(items, technicianId, ct),
                _ => []
            };
        }

        items = items.Select(t => FilterInternalComments(t, scope)).ToList();

        return new DtoTicketPage
        {
            items = items,
            total = items.Count,
            page = response.Page,
            pageSize = response.PageSize
        };
    }

    public async Task<DtoTicket?> UpdateAsync(Guid id, UpdateTicketDto request, TicketAccessScope scope, CancellationToken ct = default)
    {
        if (await GetAsync(id, scope, ct) is null) return null;

        var updated = await NotFoundAsNull(async () => Map(await client.UpdateTicketAsync(new UpdateTicketRequest
        {
            Id = id.ToString(),
            Title = request.title ?? string.Empty,
            Description = request.description ?? string.Empty,
            Status = request.status ?? string.Empty,
            Priority = request.priority ?? string.Empty
        }, cancellationToken: ct)));

        return updated is null ? null : FilterInternalComments(updated, scope);
    }

    public async Task<DtoTicket?> CloseAsync(Guid id, TicketAccessScope scope, CancellationToken ct = default)
    {
        if (await GetAsync(id, scope, ct) is null) return null;

        var closed = await NotFoundAsNull(async () =>
            Map(await client.CloseTicketAsync(new TicketRequest { Id = id.ToString() }, cancellationToken: ct)));

        return closed is null ? null : FilterInternalComments(closed, scope);
    }

    public async Task<DtoTicket?> AddCommentAsync(Guid id, AddCommentDto request, TicketAccessScope scope, CancellationToken ct = default)
    {
        if (await GetAsync(id, scope, ct) is null) return null;

        var commented = await NotFoundAsNull(async () => Map(await client.AddCommentAsync(new AddCommentRequest
        {
            TicketId = id.ToString(),
            Author = request.author ?? string.Empty,
            Body = request.body ?? string.Empty,
            IsInternal = request.isInternal
        }, cancellationToken: ct)));

        return commented is null ? null : FilterInternalComments(commented, scope);
    }

    private async Task<bool> CanSeeAsync(DtoTicket ticket, TicketAccessScope scope, CancellationToken ct)
    {
        if (scope.Role == "BaseRole") return scope.ClientFilter is not { } filter || ticket.clientId == filter;
        if (ticket.clientId != scope.ClientId) return false;

        switch (scope.Role)
        {
            case "Admin":
                return true;
            case "Support":
                return ticket.requestedByUserId == scope.CallerId;
            case "Technician" when scope.TechnicianId is { } technicianId:
                var assignment = await assignments.GetForTicketAsync(ticket.id, ct);
                return assignment?.technicianId == technicianId;
            default:
                return false;
        }
    }

    private async Task<List<DtoTicket>> FilterToAssignedAsync(List<DtoTicket> items, Guid technicianId, CancellationToken ct)
    {
        // Already scoped exactly to one Technician - no Client filter needed on top of that,
        // so this passes a BaseRole/no-filter scope purely to satisfy ListAssignmentsAsync's
        // signature, not because a real caller's role/Client matters here.
        var noFilterScope = new TenantScope(Guid.Empty, "BaseRole", null, null);
        var assigned = await assignments.ListAssignmentsAsync(technicianId, page: 1, pageSize: 500, noFilterScope, ct);
        var assignedTicketIds = assigned.Select(a => a.ticketId).ToHashSet();
        return items.Where(t => assignedTicketIds.Contains(t.id)).ToList();
    }

    private DtoTicket Map(TicketResponse response)
    {
        var dto = mapper.Map<DtoTicket>(response);
        dto.comments = response.Comments.Select(mapper.Map<DtoComment>).ToList();
        return dto;
    }

    /// <summary>Ticket.API has no concept of roles (see the class summary) so it happily returns
    /// every comment, isInternal or not, to whoever asks - this is the one place that knows who's
    /// asking. Support is this app's only "requester" role (CanSeeAsync scopes it to the ticket's
    /// own creator); everyone else who can reach a ticket - Admin, BaseRole, the assigned
    /// Technician - is staff. An isInternal comment exists specifically to talk about a ticket
    /// without the person who filed it reading it, so it must never round-trip back to them.</summary>
    private static DtoTicket FilterInternalComments(DtoTicket ticket, TicketAccessScope scope)
    {
        if (scope.Role != "Support") return ticket;

        ticket.comments = ticket.comments.Where(c => !c.isInternal).ToList();
        return ticket;
    }

    /// <summary>
    /// gRPC NotFound is not an exceptional condition at the REST boundary - it is a 404.
    /// Translating it here keeps every controller free of try/catch.
    /// </summary>
    internal static async Task<T?> NotFoundAsNull<T>(Func<Task<T>> call) where T : class
    {
        try
        {
            return await call();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
