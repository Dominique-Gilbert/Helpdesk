using AutoMapper;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Helpdesk.Sla.Grpc;

namespace Helpdesk.Gateway.Interfacing.Services;

/// <summary>
/// SLA.API has no concept of roles or who's assigned - same reasoning as TicketService, this is
/// the one place that already talks to both, so the visibility rule for "whose clocks can this
/// caller see" lives here rather than duplicated into SLA.API's own domain.
/// </summary>
public class SlaService(SlaGrpc.SlaGrpcClient client, ITicketService tickets, IMapper mapper) : ISlaService
{
    public async Task<DtoSlaClock?> GetForTicketAsync(Guid ticketId, TicketAccessScope scope, CancellationToken ct = default)
    {
        // Same "not found, not forbidden" reasoning as TicketService.GetAsync - a clock for a
        // ticket you can't see should read identically to no clock existing at all.
        if (await tickets.GetAsync(ticketId, scope, ct) is null) return null;

        // No exception to catch any more - SLA.API signals "no clock yet" (routine for a
        // freshly-created ticket whose consumer hasn't run) via Found=false on the response
        // itself. See SlaClockResponse.found's own doc comment in the .proto.
        var response = await client.GetSlaClockForTicketAsync(
            new TicketSlaRequest { TicketId = ticketId.ToString() }, cancellationToken: ct);
        return response.Found ? Map(response) : null;
    }

    public async Task<IReadOnlyList<DtoSlaClock>> ListAsync(string? status, int page, int pageSize, TicketAccessScope scope, CancellationToken ct = default)
    {
        var response = await client.ListSlaClocksAsync(new ListSlaClocksRequest
        {
            Status = status ?? string.Empty,
            Page = page,
            PageSize = pageSize
        }, cancellationToken: ct);

        var clocks = response.Clocks.Select(Map).ToList();
        if (scope.Role == "BaseRole" && scope.ClientFilter is null) return clocks;

        // Reuses ITicketService's own scoping rather than re-deriving "which tickets are mine"
        // here - one rule, same place, for whichever role is asking (Admin included, now that
        // Admin is scoped to their own Client too).
        var visibleTicketIds = (await tickets.ListAsync(null, null, page: 1, pageSize: 500, scope, ct))
            .items.Select(t => t.id).ToHashSet();

        return clocks.Where(c => visibleTicketIds.Contains(c.ticketId)).ToList();
    }

    public async Task<IReadOnlyList<DtoEscalation>> ListEscalationsAsync(Guid? slaClockId, CancellationToken ct = default)
    {
        var response = await client.ListEscalationsAsync(new ListEscalationsRequest
        {
            SlaClockId = slaClockId?.ToString() ?? string.Empty
        }, cancellationToken: ct);

        return response.Escalations.Select(mapper.Map<DtoEscalation>).ToList();
    }

    public Task<DtoSlaClock?> StopAsync(Guid ticketId, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () => Map(
            await client.StopSlaClockAsync(
                new TicketSlaRequest { TicketId = ticketId.ToString() }, cancellationToken: ct)));

    public Task<DtoSlaClock?> AdjustAsync(Guid ticketId, AdjustSlaClockDto request, string adjustedBy, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () => Map(
            await client.AdjustSlaClockAsync(new AdjustSlaClockRequest
            {
                TicketId = ticketId.ToString(),
                DeltaMinutes = request.deltaMinutes,
                Reason = request.reason,
                AdjustedBy = adjustedBy
            }, cancellationToken: ct)));

    private DtoSlaClock Map(SlaClockResponse response)
    {
        var dto = mapper.Map<DtoSlaClock>(response);
        dto.escalations = response.Escalations.Select(mapper.Map<DtoEscalation>).ToList();
        dto.adjustments = response.Adjustments.Select(mapper.Map<DtoSlaAdjustment>).ToList();
        return dto;
    }
}
