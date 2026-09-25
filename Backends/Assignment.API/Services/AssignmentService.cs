using AutoMapper;
using Grpc.Core;
using Helpdesk.Assignments.Domain.Model;
using Helpdesk.Assignments.Grpc;
using Helpdesk.Assignments.Infrastructure;
using Helpdesk.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Api.Services;

public class AssignmentService(
    AssignmentContext db,
    IMapper mapper,
    BacklogDrainService backlog) : AssignmentGrpc.AssignmentGrpcBase
{
    // GetAssignmentForTicket and ListAssignments deliberately have no [Authorize] here, unlike
    // every other method below - they're called in-process by the Gateway's TicketService and
    // TicketOverviewService on behalf of Technician/Support callers too (a Technician's "My
    // Tickets" filter, the shared Overview panel), forwarded with that caller's own token per
    // AddBearerForwarding. Gating them to Admin would silently break both.
    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<TechnicianResponse> GetTechnician(TechnicianRequest request, ServerCallContext context)
    {
        var id = ParseId(request.Id, "technician");
        var technician = await db.Technicians.AsNoTracking().Include(t => t.CategoryLevels)
            .FirstOrDefaultAsync(t => t.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Technician '{request.Id}' was not found."));

        return ToResponse(technician);
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<TechnicianListResponse> ListTechnicians(ListTechniciansRequest request, ServerCallContext context)
    {
        var query = db.Technicians.AsNoTracking().Include(t => t.CategoryLevels).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Team))
        {
            query = query.Where(t => t.Team == request.Team);
        }

        if (request.OnlyWithCapacity)
        {
            query = query.Where(t => t.IsAvailable && t.ActiveAssignments < t.MaxConcurrent);
        }

        var technicians = await query.OrderBy(t => t.FullName).ToListAsync(context.CancellationToken);

        var response = new TechnicianListResponse { Total = technicians.Count };
        response.Technicians.AddRange(technicians.Select(ToResponse));
        return response;
    }

    /// <summary>Admin-only, enforced here as well as at the Gateway - no blind trust of the Gateway.</summary>
    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<TechnicianResponse> CreateTechnician(CreateTechnicianRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Full name is required."));
        ValidateLength(request.FullName, FullNameMaxLength, nameof(request.FullName));
        ValidateLength(request.Email, EmailMaxLength, nameof(request.Email));
        ValidateLength(request.Team, TeamMaxLength, nameof(request.Team));

        var technician = new Technician
        {
            FullName = request.FullName.Trim(),
            Email = request.Email,
            Team = request.Team,
            Skills = request.Skills.ToList(),
            MaxConcurrent = request.MaxConcurrent > 0 ? request.MaxConcurrent : 5,
            ClientId = ProtoConverters.ToNullableGuid(request.ClientId)
        };
        technician.CategoryLevels = ToCategoryLevels(request.CategoryLevels, technician.Id);

        db.Technicians.Add(technician);
        await db.SaveChangesAsync(context.CancellationToken);

        // A brand-new technician is somewhere a queued ticket might now be able to go -
        // same reasoning as UpdateTechnician below.
        await backlog.DrainAsync(context.CancellationToken);

        return ToResponse(technician);
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<TechnicianResponse> UpdateTechnician(UpdateTechnicianRequest request, ServerCallContext context)
    {
        var id = ParseId(request.Id, "technician");
        var technician = await db.Technicians.Include(t => t.CategoryLevels)
            .FirstOrDefaultAsync(t => t.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Technician '{request.Id}' was not found."));

        ValidateLength(request.Email, EmailMaxLength, nameof(request.Email));
        ValidateLength(request.Team, TeamMaxLength, nameof(request.Team));
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            ValidateLength(request.FullName, FullNameMaxLength, nameof(request.FullName));
            technician.FullName = request.FullName.Trim();
        }
        technician.Email = request.Email;
        technician.Team = request.Team;
        technician.Skills = request.Skills.ToList();
        if (request.MaxConcurrent > 0) technician.MaxConcurrent = request.MaxConcurrent;
        technician.IsAvailable = request.IsAvailable;
        technician.ClientId = ProtoConverters.ToNullableGuid(request.ClientId);
        technician.UpdatedAtUtc = DateTime.UtcNow;

        // Full replace, same as Skills above - the caller always sends the complete set.
        //
        // Added directly on the DbSet, not via technician.CategoryLevels = ... - the latter is
        // navigation-fixup on an already-tracked parent, and since this entity's Guid Id is a
        // non-default client-generated key, EF Core's change tracker treats it as Modified (an
        // existing row) rather than Added, generating an UPDATE for a row that doesn't exist yet
        // and throwing DbUpdateConcurrencyException. Same pattern Asset.API's AssignAsset uses
        // (db.AssignmentRecords.Add(...), not asset.AssignmentRecords.Add(...)).
        db.TechnicianCategoryLevels.RemoveRange(technician.CategoryLevels);
        db.TechnicianCategoryLevels.AddRange(ToCategoryLevels(request.CategoryLevels, technician.Id));

        await db.SaveChangesAsync(context.CancellationToken);

        // A level going from None to something real, availability flipping on, or more
        // capacity being granted are all exactly the "somewhere to go" moments a queued
        // backlog ticket has been waiting on - don't make it wait for the technician's next
        // ticket close on top of that.
        await backlog.DrainAsync(context.CancellationToken);

        return ToResponse(technician);
    }

    private static List<TechnicianCategoryLevel> ToCategoryLevels(
        IEnumerable<TechnicianCategoryLevelMessage> messages, Guid technicianId) =>
        messages
            .Where(m => !string.IsNullOrWhiteSpace(m.Category))
            .Select(m => new TechnicianCategoryLevel
            {
                TechnicianId = technicianId,
                Category = m.Category.Trim(),
                Level = ProtoConverters.ToEnum(m.Level, SkillLevel.Normal)
            })
            .ToList();

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<RoutingRuleListResponse> ListRoutingRules(ListRoutingRulesRequest request, ServerCallContext context)
    {
        var query = db.RoutingRules.AsNoTracking().AsQueryable();
        if (request.OnlyActive) query = query.Where(r => r.IsActive);

        var rules = await query
            .OrderBy(r => r.Rank)
            .ThenBy(r => r.Name)
            .ToListAsync(context.CancellationToken);

        var response = new RoutingRuleListResponse { Total = rules.Count };
        response.Rules.AddRange(rules.Select(mapper.Map<RoutingRuleResponse>));
        return response;
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<RoutingRuleResponse> CreateRoutingRule(CreateRoutingRuleRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required."));
        if (string.IsNullOrWhiteSpace(request.Team))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Team is required."));
        ValidateLength(request.Name, RuleNameMaxLength, nameof(request.Name));
        ValidateLength(request.Category, CategoryMaxLength, nameof(request.Category));
        ValidateLength(request.Team, TeamMaxLength, nameof(request.Team));

        var rule = new RoutingRule
        {
            Name = request.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            MinPriority = ProtoConverters.ToEnum(request.MinPriority, TicketPriority.Low),
            Team = request.Team.Trim(),
            Rank = request.Rank,
            IsActive = true,
            ClientId = ProtoConverters.ToNullableGuid(request.ClientId)
        };

        db.RoutingRules.Add(rule);
        await db.SaveChangesAsync(context.CancellationToken);

        return mapper.Map<RoutingRuleResponse>(rule);
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<RoutingRuleResponse> UpdateRoutingRule(UpdateRoutingRuleRequest request, ServerCallContext context)
    {
        var id = ParseId(request.Id, "routing rule");
        var rule = await db.RoutingRules.FirstOrDefaultAsync(r => r.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Routing rule '{request.Id}' was not found."));

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required."));
        if (string.IsNullOrWhiteSpace(request.Team))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Team is required."));
        ValidateLength(request.Name, RuleNameMaxLength, nameof(request.Name));
        ValidateLength(request.Category, CategoryMaxLength, nameof(request.Category));
        ValidateLength(request.Team, TeamMaxLength, nameof(request.Team));

        rule.Name = request.Name.Trim();
        rule.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        rule.MinPriority = ProtoConverters.ToEnum(request.MinPriority, rule.MinPriority);
        rule.Team = request.Team.Trim();
        rule.Rank = request.Rank;
        rule.IsActive = request.IsActive;
        rule.ClientId = ProtoConverters.ToNullableGuid(request.ClientId);
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(context.CancellationToken);

        return mapper.Map<RoutingRuleResponse>(rule);
    }

    public override async Task<AssignmentResponse> GetAssignmentForTicket(TicketAssignmentRequest request, ServerCallContext context)
    {
        var ticketId = ParseId(request.TicketId, "ticket");

        var assignment = await db.TicketAssignments.AsNoTracking()
            .Include(a => a.Technician)
            .Include(a => a.RoutingRule)
            .FirstOrDefaultAsync(a => a.TicketId == ticketId, context.CancellationToken);

        // No assignment yet is an expected, common state - the ticket is sitting in the
        // backlog (nobody had capacity at creation time) or the TicketCreated consumer just
        // hasn't run yet. Not an error, so not an RpcException here: throwing NotFound for
        // something this routine meant every "load a ticket's assignment" call threw and got
        // caught as a matter of course, and - because gRPC's own exception-to-status handling
        // lives in framework code, not this method's own try/catch - a debugger attached to
        // this process broke on it as "User-Unhandled" every single time.
        if (assignment is null) return new AssignmentResponse { Found = false };

        var response = mapper.Map<AssignmentResponse>(assignment);
        response.Found = true;
        return response;
    }

    public override async Task<AssignmentListResponse> ListAssignments(ListAssignmentsRequest request, ServerCallContext context)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

        var query = db.TicketAssignments.AsNoTracking()
            .Include(a => a.Technician)
            .Include(a => a.RoutingRule)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.TechnicianId))
        {
            var technicianId = ParseId(request.TechnicianId, "technician");
            query = query.Where(a => a.TechnicianId == technicianId);
        }

        if (!string.IsNullOrWhiteSpace(request.ClientId))
        {
            var clientId = ParseId(request.ClientId, "client");
            query = query.Where(a => a.Technician!.ClientId == clientId);
        }

        var total = await query.CountAsync(context.CancellationToken);
        var assignments = await query
            .OrderByDescending(a => a.AssignedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new AssignmentListResponse { Total = total };
        response.Assignments.AddRange(assignments.Select(a =>
        {
            var item = mapper.Map<AssignmentResponse>(a);
            item.Found = true;
            return item;
        }));
        return response;
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<BacklogListResponse> ListBacklog(ListBacklogRequest request, ServerCallContext context)
    {
        var query = db.BacklogEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(b => b.Category == request.Category);
        }

        var entries = await query.OrderBy(b => b.QueuedAtUtc).ToListAsync(context.CancellationToken);

        var response = new BacklogListResponse { Total = entries.Count };
        response.Entries.AddRange(entries.Select(mapper.Map<BacklogEntryResponse>));
        return response;
    }

    private TechnicianResponse ToResponse(Technician technician)
    {
        var response = mapper.Map<TechnicianResponse>(technician);
        response.Skills.AddRange(technician.Skills);
        response.CategoryLevels.AddRange(technician.CategoryLevels.Select(mapper.Map<TechnicianCategoryLevelMessage>));
        return response;
    }

    private static Guid ParseId(string raw, string label) =>
        Guid.TryParse(raw, out var id) && id != Guid.Empty
            ? id
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{raw}' is not a valid {label} id."));

    // Mirrors AssignmentContext's HasMaxLength calls - checked here too so an oversized field
    // fails with a clean 400 instead of reaching SaveChangesAsync and throwing an unhandled
    // DbUpdateException, which the Gateway can only surface as a bare 500 (same bug class fixed
    // in Ticket.API, Asset.API and Client.API this round).
    private const int FullNameMaxLength = 128;
    private const int EmailMaxLength = 256;
    private const int TeamMaxLength = 64;
    private const int RuleNameMaxLength = 128;
    private const int CategoryMaxLength = 64;

    private static void ValidateLength(string? value, int maxLength, string fieldName)
    {
        if (value is { Length: var length } && length > maxLength)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"{fieldName} must not exceed {maxLength} characters (was {length})."));
        }
    }
}
