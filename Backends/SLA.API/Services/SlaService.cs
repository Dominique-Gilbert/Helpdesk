using AutoMapper;
using Grpc.Core;
using Helpdesk.Sla.Domain.Model;
using Helpdesk.Sla.Domain.Sla;
using Helpdesk.Sla.Grpc;
using Helpdesk.Sla.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Sla.Api.Services;

public class SlaService(SlaContext db, IMapper mapper) : SlaGrpc.SlaGrpcBase
{
    // GetSlaClockForTicket deliberately has no [Authorize] here, unlike everything else below -
    // it's called in-process by the Gateway's TicketOverviewService on behalf of every role (the
    // shared Tickets/Completed Tickets Overview panel), forwarded with that caller's own token
    // per AddBearerForwarding. Gating it to Admin would silently break Overview for non-Admins.
    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<SlaClockResponse> GetSlaClock(SlaClockRequest request, ServerCallContext context)
    {
        var id = ParseId(request.Id, "SLA clock");
        var clock = await Query().FirstOrDefaultAsync(c => c.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"SLA clock '{request.Id}' was not found."));

        return ToResponse(clock);
    }

    public override async Task<SlaClockResponse> GetSlaClockForTicket(TicketSlaRequest request, ServerCallContext context)
    {
        var ticketId = ParseId(request.TicketId, "ticket");
        var clock = await Query().FirstOrDefaultAsync(c => c.TicketId == ticketId, context.CancellationToken);

        // No clock yet is an expected, common state (a just-created ticket's TicketCreated
        // consumer hasn't run, or the ticket is still queued in the backlog) - not an error, so
        // it is not an RpcException here. Throwing NotFound for something this routine would
        // mean every "load a ticket's SLA info" call throws and gets caught as a matter of
        // course, which is both needless overhead and - because gRPC's own exception-to-status
        // handling lives in framework code, not in this method's own try/catch - something a
        // debugger attached to this process breaks on as "User-Unhandled" every single time.
        return clock is null ? new SlaClockResponse { Found = false } : ToResponse(clock);
    }

    // Also unguarded, same reason as GetSlaClockForTicket above: the Gateway's own SlaService
    // calls this on behalf of every role (a Technician/Support caller's own "My Tickets" SLA
    // chips), then does the real per-caller filtering itself before returning.
    public override async Task<SlaClockListResponse> ListSlaClocks(ListSlaClocksRequest request, ServerCallContext context)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

        var query = Query();
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<SlaClockStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(c => c.Status == status);
        }

        var total = await query.CountAsync(context.CancellationToken);
        var clocks = await query
            .OrderBy(c => c.DueAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new SlaClockListResponse { Total = total };
        response.Clocks.AddRange(clocks.Select(ToResponse));
        return response;
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<EscalationListResponse> ListEscalations(ListEscalationsRequest request, ServerCallContext context)
    {
        var query = db.Escalations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SlaClockId))
        {
            var clockId = ParseId(request.SlaClockId, "SLA clock");
            query = query.Where(e => e.SlaClockId == clockId);
        }

        var escalations = await query
            .OrderByDescending(e => e.RaisedAtUtc)
            .Take(200)
            .ToListAsync(context.CancellationToken);

        var response = new EscalationListResponse { Total = escalations.Count };
        response.Escalations.AddRange(escalations.Select(mapper.Map<EscalationMessage>));
        return response;
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<SlaClockResponse> StopSlaClock(TicketSlaRequest request, ServerCallContext context)
    {
        var ticketId = ParseId(request.TicketId, "ticket");
        var clock = await db.SlaClocks
            .Include(c => c.Escalations)
            .FirstOrDefaultAsync(c => c.TicketId == ticketId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound,
                $"No SLA clock exists for ticket '{request.TicketId}'."));

        if (clock.Status != SlaClockStatus.Stopped)
        {
            SlaClockCalculator.Stop(clock, DateTime.UtcNow);
            await db.SaveChangesAsync(context.CancellationToken);
        }

        return ToResponse(clock);
    }

    /// <summary>Admin-only, enforced here as well as at the Gateway - no blind trust of the Gateway.</summary>
    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<SlaClockResponse> AdjustSlaClock(AdjustSlaClockRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "A reason is required."));
        if (request.DeltaMinutes == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Delta minutes must not be zero."));

        var ticketId = ParseId(request.TicketId, "ticket");
        var clock = await db.SlaClocks
            .Include(c => c.Escalations)
            .Include(c => c.Adjustments)
            .FirstOrDefaultAsync(c => c.TicketId == ticketId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound,
                $"No SLA clock exists for ticket '{request.TicketId}'."));

        var now = DateTime.UtcNow;

        try
        {
            SlaClockCalculator.Adjust(clock, TimeSpan.FromMinutes(request.DeltaMinutes), now);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        // db.Adjustments.Add(...), not clock.Adjustments.Add(...) - SlaAdjustment.Id is a
        // client-generated Guid (not store-generated), so EF's change tracker can only tell
        // this is a brand new row, rather than treating it as an existing one to UPDATE (which
        // affects 0 rows and throws DbUpdateConcurrencyException), when it is added to the
        // DbSet directly. Same convention Ticket.API's Comment already uses.
        db.Adjustments.Add(new SlaAdjustment
        {
            SlaClockId = clock.Id,
            DeltaMinutes = request.DeltaMinutes,
            Reason = request.Reason.Trim(),
            AdjustedBy = request.AdjustedBy,
            AdjustedAtUtc = now
        });

        await db.SaveChangesAsync(context.CancellationToken);

        return ToResponse(clock);
    }

    private IQueryable<SlaClock> Query() =>
        db.SlaClocks.AsNoTracking().Include(c => c.Escalations).Include(c => c.Adjustments);

    private SlaClockResponse ToResponse(SlaClock clock)
    {
        var now = DateTime.UtcNow;
        var response = mapper.Map<SlaClockResponse>(clock);
        response.Found = true;
        response.RemainingSeconds = (long)SlaClockCalculator.Remaining(clock, now).TotalSeconds;
        response.EscalationLevel = SlaClockCalculator.EscalationLevel(clock, now);
        response.Escalations.AddRange(
            clock.Escalations.OrderBy(e => e.Level).Select(mapper.Map<EscalationMessage>));
        response.Adjustments.AddRange(
            clock.Adjustments.OrderByDescending(a => a.AdjustedAtUtc).Select(mapper.Map<SlaAdjustmentMessage>));
        return response;
    }

    private static Guid ParseId(string raw, string label) =>
        Guid.TryParse(raw, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{raw}' is not a valid {label} id."));
}
