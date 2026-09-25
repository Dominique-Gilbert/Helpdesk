using AutoMapper;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;

namespace Helpdesk.Gateway.Interfacing.Services;

public class AssignmentService(AssignmentGrpc.AssignmentGrpcClient client, IMapper mapper, IClientService clients) : IAssignmentService
{
    public async Task<IReadOnlyList<DtoTechnician>> ListTechniciansAsync(string? team, bool onlyWithCapacity, TenantScope scope, CancellationToken ct = default)
    {
        var response = await client.ListTechniciansAsync(new ListTechniciansRequest
        {
            Team = team ?? string.Empty,
            OnlyWithCapacity = onlyWithCapacity
        }, cancellationToken: ct);

        var items = response.Technicians.Select(Map).ToList();

        // Same rule as AssetService.ListAsync/TicketService.ListAsync.
        return scope.Role == "BaseRole"
            ? scope.ClientFilter is { } filter ? items.Where(t => t.clientId == filter).ToList() : items
            : items.Where(t => t.clientId == scope.ClientId).ToList();
    }

    public async Task<DtoTechnician?> GetTechnicianAsync(Guid id, TenantScope scope, CancellationToken ct = default)
    {
        var technician = await TicketService.NotFoundAsNull(async () =>
            Map(await client.GetTechnicianAsync(new TechnicianRequest { Id = id.ToString() }, cancellationToken: ct)));

        if (technician is null) return null;

        var visible = scope.Role == "BaseRole"
            ? scope.ClientFilter is not { } filter || technician.clientId == filter
            : technician.clientId == scope.ClientId;

        return visible ? technician : null;
    }

    public async Task<DtoTechnician> CreateTechnicianAsync(CreateTechnicianDto request, CancellationToken ct = default)
    {
        var grpcRequest = new CreateTechnicianRequest
        {
            FullName = request.fullName,
            Email = request.email,
            Team = request.team,
            MaxConcurrent = request.maxConcurrent,
            ClientId = request.clientId?.ToString() ?? string.Empty
        };
        grpcRequest.Skills.AddRange(request.skills);
        grpcRequest.CategoryLevels.AddRange(request.categoryLevels.Select(ToLevelMessage));

        return Map(await client.CreateTechnicianAsync(grpcRequest, cancellationToken: ct));
    }

    public Task<DtoTechnician?> UpdateTechnicianAsync(Guid id, UpdateTechnicianDto request, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var grpcRequest = new UpdateTechnicianRequest
            {
                Id = id.ToString(),
                FullName = request.fullName,
                Email = request.email,
                Team = request.team,
                MaxConcurrent = request.maxConcurrent,
                IsAvailable = request.isAvailable,
                ClientId = request.clientId?.ToString() ?? string.Empty
            };
            grpcRequest.Skills.AddRange(request.skills);
            grpcRequest.CategoryLevels.AddRange(request.categoryLevels.Select(ToLevelMessage));

            return Map(await client.UpdateTechnicianAsync(grpcRequest, cancellationToken: ct));
        });

    public async Task<IReadOnlyList<DtoRoutingRule>> ListRoutingRulesAsync(bool onlyActive, TenantScope scope, CancellationToken ct = default)
    {
        var response = await client.ListRoutingRulesAsync(
            new ListRoutingRulesRequest { OnlyActive = onlyActive }, cancellationToken: ct);

        var items = response.Rules.Select(mapper.Map<DtoRoutingRule>).ToList();

        // Unlike Technicians/Users, a null ClientId here is a *global* rule - it still matches
        // every Client's tickets in RoutingRuleMatcher, which does not filter candidates by
        // Client at all. Hiding it from a scoped Admin would make a rule that is actively
        // routing their tickets invisible (and unexplainable) to them, so it stays in view
        // alongside whichever Client the caller is scoped/filtered to.
        return scope.Role == "BaseRole"
            ? scope.ClientFilter is { } filter ? items.Where(r => r.clientId == filter || r.clientId is null).ToList() : items
            : items.Where(r => r.clientId == scope.ClientId || r.clientId is null).ToList();
    }

    public async Task<DtoRoutingRule> CreateRoutingRuleAsync(CreateRoutingRuleDto request, TenantScope scope, CancellationToken ct = default)
    {
        var clientId = await ResolveClientIdAsync(request.clientId, scope, ct);

        var response = await client.CreateRoutingRuleAsync(new CreateRoutingRuleRequest
        {
            Name = request.name,
            Category = request.category ?? string.Empty,
            MinPriority = request.minPriority,
            Team = request.team,
            Rank = request.rank,
            ClientId = clientId?.ToString() ?? string.Empty
        }, cancellationToken: ct);

        return mapper.Map<DtoRoutingRule>(response);
    }

    public Task<DtoRoutingRule?> UpdateRoutingRuleAsync(Guid id, UpdateRoutingRuleDto request, TenantScope scope, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var clientId = await ResolveClientIdAsync(request.clientId, scope, ct);

            return mapper.Map<DtoRoutingRule>(
                await client.UpdateRoutingRuleAsync(new UpdateRoutingRuleRequest
                {
                    Id = id.ToString(),
                    Name = request.name,
                    Category = request.category ?? string.Empty,
                    MinPriority = request.minPriority,
                    Team = request.team,
                    Rank = request.rank,
                    IsActive = request.isActive,
                    ClientId = clientId?.ToString() ?? string.Empty
                }, cancellationToken: ct));
        });

    // Same rule as UserService.ResolveClientIdForCreateAsync: only BaseRole may pick a Client
    // (or none, for a rule that applies everywhere) - anyone else's request value is ignored
    // and forced to their own Client.
    private async Task<Guid?> ResolveClientIdAsync(Guid? requested, TenantScope scope, CancellationToken ct)
    {
        if (scope.Role != "BaseRole") return scope.ClientId;

        var clientId = requested is null || requested == Guid.Empty ? null : requested;
        if (clientId is { } id && await clients.GetAsync(id, ct) is null)
        {
            throw new InvalidOperationException($"Client '{id}' was not found.");
        }

        return clientId;
    }

    public async Task<DtoAssignment?> GetForTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        // No exception to catch any more - Assignment.API signals "not yet assigned" (a
        // ticket still queued in the backlog is routine, not exceptional) via Found=false on
        // the response itself. See AssignmentResponse.found's own doc comment in the .proto.
        var response = await client.GetAssignmentForTicketAsync(
            new TicketAssignmentRequest { TicketId = ticketId.ToString() }, cancellationToken: ct);
        return response.Found ? mapper.Map<DtoAssignment>(response) : null;
    }

    public async Task<IReadOnlyList<DtoAssignment>> ListAssignmentsAsync(Guid? technicianId, int page, int pageSize, TenantScope scope, CancellationToken ct = default)
    {
        // Filtered server-side (not post-fetched, unlike Technicians/RoutingRules) since this
        // one is genuinely paginated - post-filtering a page would silently under-fill it.
        // For a non-BaseRole caller with no ClientId of their own (shouldn't happen in practice,
        // but fail closed rather than open), Guid.Empty never matches a real Client - unlike
        // an empty proto string, which this field's own convention reads as "no filter".
        var clientId = scope.Role == "BaseRole" ? scope.ClientFilter : (scope.ClientId ?? Guid.Empty);

        var response = await client.ListAssignmentsAsync(new ListAssignmentsRequest
        {
            TechnicianId = technicianId?.ToString() ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            ClientId = clientId?.ToString() ?? string.Empty
        }, cancellationToken: ct);

        return response.Assignments.Select(mapper.Map<DtoAssignment>).ToList();
    }

    public async Task<IReadOnlyList<DtoBacklogEntry>> ListBacklogAsync(string? category, CancellationToken ct = default)
    {
        var response = await client.ListBacklogAsync(
            new ListBacklogRequest { Category = category ?? string.Empty }, cancellationToken: ct);

        return response.Entries.Select(mapper.Map<DtoBacklogEntry>).ToList();
    }

    private DtoTechnician Map(TechnicianResponse response)
    {
        var dto = mapper.Map<DtoTechnician>(response);
        dto.skills = response.Skills.ToList();
        dto.categoryLevels = response.CategoryLevels.Select(mapper.Map<DtoTechnicianCategoryLevel>).ToList();
        return dto;
    }

    private static TechnicianCategoryLevelMessage ToLevelMessage(DtoTechnicianCategoryLevel level) => new()
    {
        Category = level.category,
        Level = level.level
    };
}
