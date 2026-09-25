using Helpdesk.Tickets.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Tickets.Infrastructure.Data;

/// <summary>
/// Deliberately NOT a generic IRepository&lt;T&gt;. Plain CRUD stays in the gRPC service
/// against the DbContext; this thin class exists only for the two reads that more than
/// one call site needs, so the include-graph is defined once.
/// </summary>
public class TicketRepo(TicketContext context)
{
    public Task<Ticket?> GetWithCommentsAsync(Guid id, CancellationToken ct = default) =>
        context.Tickets
            .Include(t => t.Comments.OrderBy(c => c.CreatedAtUtc))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<Ticket>> ListAsync(
        TicketStatus? status,
        TicketPriority? priority,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = ApplyStatusFilter(context.Tickets.Include(t => t.Comments).AsQueryable(), status);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);

        return await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(TicketStatus? status, TicketPriority? priority, CancellationToken ct = default)
    {
        var query = ApplyStatusFilter(context.Tickets.AsQueryable(), status);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        return query.CountAsync(ct);
    }

    /// <summary>
    /// An explicit status filter is honoured exactly (including "give me Completed"). With no
    /// filter, the caller wants the working set - Completed tickets have already sat closed for
    /// 24h with no follow-up and belong in the Completed Tickets list only, not the main board.
    /// </summary>
    private static IQueryable<Ticket> ApplyStatusFilter(IQueryable<Ticket> query, TicketStatus? status) =>
        status.HasValue
            ? query.Where(t => t.Status == status.Value)
            : query.Where(t => t.Status != TicketStatus.Completed);
}
