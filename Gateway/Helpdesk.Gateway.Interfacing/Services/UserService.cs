using AutoMapper;
using Grpc.Core;
using Helpdesk.Gateway.Dto;
using Helpdesk.Gateway.Interfacing.Interfaces;
using Helpdesk.Mapping;
using Helpdesk.Users.Grpc;

namespace Helpdesk.Gateway.Interfacing.Services;

/// <summary>
/// Also orchestrates Assignment.API for Technician-role Users: a Technician-role User always
/// has a real Technician record behind it (never a link to a pre-existing one picked from a
/// list), so creating/updating such a User also creates/updates that Technician here, in the
/// one place that already understands both sides of the link.
/// </summary>
public class UserService(UserGrpc.UserGrpcClient client, IAssignmentService assignments, IClientService clients, IMapper mapper) : IUserService
{
    /// <summary>Bad credentials surface as the gRPC Unauthenticated status, which the gateway's
    /// global exception handler already maps to a 401 - nothing extra needed here.</summary>
    public async Task<DtoLoginResult?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var response = await client.LoginAsync(
            new LoginRequest { Username = request.username, Password = request.password },
            cancellationToken: ct);
        return mapper.Map<DtoLoginResult>(response);
    }

    /// <summary>Post-filtered, not pushed down into the gRPC request - same reasoning as
    /// TicketService.ListAsync's own role-based filtering. BaseRole sees every User, optionally
    /// narrowed to one Client at a time (the "switch between clients" picker); everyone else who
    /// can reach this endpoint (Admin) only ever sees their own Client's Users, clientFilter or
    /// not - an Admin has no business picking another tenant's filter value.</summary>
    public async Task<List<DtoUserSummary>> ListUsersAsync(UserCallerScope caller, Guid? clientFilter, CancellationToken ct = default)
    {
        var response = await client.ListUsersAsync(new ListUsersRequest(), cancellationToken: ct);
        var items = response.Users.Select(mapper.Map<DtoUserSummary>).ToList();

        if (caller.Role != "BaseRole") return items.Where(u => u.clientId == caller.ClientId).ToList();

        return clientFilter is { } filter ? items.Where(u => u.clientId == filter).ToList() : items;
    }

    public async Task<DtoUserSummary> CreateUserAsync(CreateUserDto request, UserCallerScope caller, CancellationToken ct = default)
    {
        var clientId = await ResolveClientIdForCreateAsync(request, caller, ct);

        Guid? technicianId = null;

        if (request.role == "Technician")
        {
            var details = request.technician ?? new TechnicianDetailsDto();
            ValidateTechnicianDetails(details);
            var technician = await assignments.CreateTechnicianAsync(new CreateTechnicianDto
            {
                fullName = request.fullName,
                email = details.email,
                team = details.team,
                skills = details.skills,
                maxConcurrent = details.maxConcurrent,
                categoryLevels = details.categoryLevels,
                clientId = clientId
            }, ct);
            technicianId = technician.id;
        }

        var response = await client.CreateUserAsync(new CreateUserRequest
        {
            Username = request.username,
            Password = request.password,
            FullName = request.fullName,
            Role = request.role,
            TechnicianId = technicianId?.ToString() ?? string.Empty,
            ClientId = clientId?.ToString() ?? string.Empty
        }, cancellationToken: ct);

        return mapper.Map<DtoUserSummary>(response);
    }

    public Task<DtoUserSummary?> UpdateUserAsync(Guid id, UpdateUserDto request, UserCallerScope caller, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var clientId = await ResolveClientIdForUpdateAsync(id, request, caller, ct);

            Guid? technicianId;
            if (request.role == "Technician")
            {
                technicianId = await CreateOrUpdateTechnicianAsync(request, clientId, ct);
            }
            else
            {
                // Role isn't Technician any more - the Technician row is deactivated below so it
                // stops being routable (same reasoning the "Available for new assignments" toggle
                // already uses for a technician taking planned leave), but unlike everywhere else
                // that drops a link outright, the User keeps pointing at it. Dropping it here would
                // mean a later role change back to Technician has no way to find this row again -
                // CreateOrUpdateTechnicianAsync would see technicianId: null and mint a brand new
                // one, leaving the deactivated original as a permanent duplicate. The TechnicianId
                // JWT claim this produces for a Support/Admin User is inert: every consumer in the
                // Gateway (CallerScopeExtensions, TicketService's ticket-scoping) gates on
                // Role == "Technician" first, so carrying a stale id grants no extra reach.
                technicianId = request.technicianId;
                if (request.technicianId is { } formerTechnicianId)
                {
                    await DeactivateTechnicianAsync(formerTechnicianId, ct);
                }
            }

            var response = await client.UpdateUserAsync(new UpdateUserRequest
            {
                Id = id.ToString(),
                FullName = request.fullName,
                Role = request.role,
                IsActive = request.isActive,
                TechnicianId = technicianId?.ToString() ?? string.Empty,
                NewPassword = request.newPassword ?? string.Empty,
                ClientId = clientId?.ToString() ?? string.Empty
            }, cancellationToken: ct);

            return mapper.Map<DtoUserSummary>(response);
        });

    /// <summary>BaseRole picks the Client (or none) for the User they're creating - "the first
    /// admin linked to a Client" is just this with role == "Admin". Anyone else's choice is
    /// discarded: their new User always inherits their own ClientId (null if they have none),
    /// never something client-supplied.</summary>
    private async Task<Guid?> ResolveClientIdForCreateAsync(CreateUserDto request, UserCallerScope caller, CancellationToken ct)
    {
        if (caller.Role != "BaseRole") return caller.ClientId;

        var clientId = NormalizeClientId(request.clientId);
        await EnsureClientExistsAsync(clientId, ct);
        return clientId;
    }

    /// <summary>Same rule as ResolveClientIdForCreateAsync, but for an existing User: only
    /// BaseRole may move a User between Clients (or unlink one entirely) - anyone else's request
    /// value is ignored and the User's current ClientId is re-sent unchanged, per
    /// UpdateUserRequest.client_id's "always the final decided value" contract.</summary>
    private async Task<Guid?> ResolveClientIdForUpdateAsync(Guid id, UpdateUserDto request, UserCallerScope caller, CancellationToken ct)
    {
        if (caller.Role == "BaseRole")
        {
            var clientId = NormalizeClientId(request.clientId);
            await EnsureClientExistsAsync(clientId, ct);
            return clientId;
        }

        var current = await client.GetUserAsync(new GetUserRequest { Id = id.ToString() }, cancellationToken: ct);
        return ProtoConverters.ToNullableGuid(current.ClientId);
    }

    // Guid.Empty is never a real Client's id (Client.API always mints a fresh Guid), so treat it
    // the same as "no Client" rather than attempting - and failing - to validate it as one.
    private static Guid? NormalizeClientId(Guid? clientId) => clientId is null || clientId == Guid.Empty ? null : clientId;

    private async Task EnsureClientExistsAsync(Guid? clientId, CancellationToken ct)
    {
        if (clientId is not { } id) return;

        if (await clients.GetAsync(id, ct) is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Client '{id}' does not exist."));
    }

    public Task<DtoUserSummary?> GetUserAsync(Guid id, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var response = await client.GetUserAsync(new GetUserRequest { Id = id.ToString() }, cancellationToken: ct);
            return mapper.Map<DtoUserSummary>(response);
        });

    public Task<DtoUserSummary?> UpdateMyProfileAsync(Guid id, UpdateMyProfileDto request, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var response = await client.UpdateMyProfileAsync(new UpdateMyProfileRequest
            {
                Id = id.ToString(),
                FullName = request.fullName,
                Email = request.email ?? string.Empty
            }, cancellationToken: ct);
            return mapper.Map<DtoUserSummary>(response);
        });

    public async Task<DtoClientBranding?> GetMyBrandingAsync(UserCallerScope caller, CancellationToken ct = default)
    {
        if (caller.ClientId is not { } clientId) return null;

        // Goes through the same branding-specific lookup as the login-hint path (GetPublicBrandingAsync),
        // rather than reconstructing from GetAsync/DtoClient - that one always resolves DisplayName's
        // "null means use Company" fallback server-side (Client.API's GetClientBranding); rebuilding
        // it here from raw Company would silently drop any override.
        return await clients.GetPublicBrandingAsync(clientId, ct);
    }

    public async Task<DtoClientBranding?> UpdateMyBrandingAsync(UserCallerScope caller, UpdateMyBrandingDto request, CancellationToken ct = default) =>
        caller.Role == "Admin" && caller.ClientId is { } clientId
            ? await clients.UpdateMyBrandingAsync(clientId, request, ct)
            : null;

    public async Task<DtoClientBranding> GetLoginBrandingAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return new DtoClientBranding();

        var hint = await client.GetLoginBrandingHintAsync(new UsernameRequest { Username = username.Trim() }, cancellationToken: ct);
        var clientId = ProtoConverters.ToNullableGuid(hint.ClientId);

        return clientId is { } id ? await clients.GetPublicBrandingAsync(id, ct) : new DtoClientBranding();
    }

    public Task<DtoUserSummary?> ChangePasswordAsync(Guid id, ChangePasswordDto request, CancellationToken ct = default) =>
        TicketService.NotFoundAsNull(async () =>
        {
            var response = await client.ChangePasswordAsync(new ChangePasswordRequest
            {
                Id = id.ToString(),
                CurrentPassword = request.currentPassword,
                NewPassword = request.newPassword
            }, cancellationToken: ct);
            return mapper.Map<DtoUserSummary>(response);
        });

    private async Task<Guid?> CreateOrUpdateTechnicianAsync(UpdateUserDto request, Guid? clientId, CancellationToken ct)
    {
        var details = request.technician ?? new TechnicianDetailsDto();
        ValidateTechnicianDetails(details);

        if (request.technicianId is { } existingId)
        {
            await assignments.UpdateTechnicianAsync(existingId, new UpdateTechnicianDto
            {
                fullName = request.fullName,
                email = details.email,
                team = details.team,
                skills = details.skills,
                maxConcurrent = details.maxConcurrent,
                isAvailable = details.isAvailable,
                categoryLevels = details.categoryLevels,
                clientId = clientId
            }, ct);
            return existingId;
        }

        // The role just changed to Technician - there's no Technician record yet.
        var created = await assignments.CreateTechnicianAsync(new CreateTechnicianDto
        {
            fullName = request.fullName,
            email = details.email,
            team = details.team,
            skills = details.skills,
            maxConcurrent = details.maxConcurrent,
            categoryLevels = details.categoryLevels,
            clientId = clientId
        }, ct);
        return created.id;
    }

    /// <summary>A User's role just changed away from Technician - the Technician row stays (it's
    /// history, same as everywhere else in this app that avoids hard deletes), but it must stop
    /// being a live routing candidate now that no User can act as it. Everything except
    /// isAvailable is round-tripped unchanged; a technician that no longer exists at all
    /// (already removed some other way) is a no-op, not an error.</summary>
    private async Task DeactivateTechnicianAsync(Guid technicianId, CancellationToken ct)
    {
        // Not a real caller's own visibility scope - this technician is already known by id
        // (it was this User's own link a moment ago), so an unfiltered BaseRole-shaped scope
        // just satisfies GetTechnicianAsync's signature rather than re-deriving "who is asking".
        var unfilteredScope = new TenantScope(Guid.Empty, "BaseRole", null, null);
        var technician = await assignments.GetTechnicianAsync(technicianId, unfilteredScope, ct);
        if (technician is null || !technician.isAvailable) return;

        // A technician still carrying live work can't just vanish from routing: deactivating them
        // here would leave their assigned ticket(s) pointed at a Technician no User can act as,
        // and with isAvailable=false nothing else in the app - not the backlog drain, not any
        // manual "reassign" tool, because none exists - would ever pick that ticket back up. Block
        // the role change instead of silently stranding it; the caller has to hand the work off
        // (reassign or close it) before the role can change away from Technician.
        if (technician.activeAssignments > 0)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"Cannot change this User's role away from Technician: '{technician.fullName}' still has " +
                $"{technician.activeAssignments} active ticket(s) assigned. Reassign or close them first."));
        }

        await assignments.UpdateTechnicianAsync(technicianId, new UpdateTechnicianDto
        {
            fullName = technician.fullName,
            email = technician.email,
            team = technician.team,
            skills = technician.skills,
            maxConcurrent = technician.maxConcurrent,
            isAvailable = false,
            categoryLevels = technician.categoryLevels,
            clientId = technician.clientId
        }, ct);
    }

    private static void ValidateTechnicianDetails(TechnicianDetailsDto details)
    {
        if (details.maxConcurrent < 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "maxConcurrent must not be negative."));
        }
    }
}
