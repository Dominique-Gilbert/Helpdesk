using Grpc.Core;
using Helpdesk.Mapping;
using Helpdesk.Users.Api.Security;
using Helpdesk.Users.Domain.Model;
using Helpdesk.Users.Grpc;
using Helpdesk.Users.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Users.Api.Services;

public class UserService(UserContext db, JwtTokenIssuer tokenIssuer) : UserGrpc.UserGrpcBase
{
    [AllowAnonymous]
    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.UserRole!)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password."));

        var verification = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password."));

        var roleName = user.UserRole?.Role?.Name
            ?? throw new RpcException(new Status(StatusCode.FailedPrecondition, $"User '{user.Username}' has no role assigned."));

        var (token, expiresAtUtc) = tokenIssuer.Issue(user, roleName);

        return new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = ProtoConverters.ToProto(expiresAtUtc),
            UserId = ProtoConverters.ToProto(user.Id),
            FullName = user.FullName,
            Role = roleName,
            TechnicianId = ProtoConverters.ToProto(user.TechnicianId)
        };
    }

    /// <summary>See the .proto's own doc comment - deliberately never distinguishes "unknown
    /// username" from "known but inactive/unlinked" from "known and linked to a branding-less
    /// Client". All three answer with the same empty client_id.</summary>
    [AllowAnonymous]
    public override async Task<UserBrandingHintResponse> GetLoginBrandingHint(UsernameRequest request, ServerCallContext context)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, context.CancellationToken);

        return new UserBrandingHintResponse { ClientId = ProtoConverters.ToProto(user?.ClientId) };
    }

    /// <summary>Admin-only, enforced here as well as at the Gateway - no blind trust of the Gateway.</summary>
    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<UserListResponse> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        var users = await db.Users.AsNoTracking()
            .Include(u => u.UserRole!)
                .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .ToListAsync(context.CancellationToken);

        var response = new UserListResponse();
        response.Users.AddRange(users.Select(u => ToSummary(u, u.UserRole?.Role?.Name ?? string.Empty)));
        return response;
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<UserSummary> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Username, password and full name are required."));
        // Same minimum ChangePassword already enforces for a self-service password change -
        // there was no reason for a brand-new account to be held to a looser bar than a change
        // to an existing one.
        if (request.Password.Length < 8)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Password must be at least 8 characters."));
        ValidateLength(request.Username, UsernameMaxLength, nameof(request.Username));
        ValidateLength(request.FullName, FullNameMaxLength, nameof(request.FullName));

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown role '{request.Role}'."));

        if (await db.Users.AnyAsync(u => u.Username == request.Username, context.CancellationToken))
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Username '{request.Username}' is already taken."));

        var user = new User
        {
            Username = request.Username.Trim(),
            FullName = request.FullName.Trim(),
            TechnicianId = ProtoConverters.ToNullableGuid(request.TechnicianId),
            // Always the Gateway's final decided value (its own choice for BaseRole, or the
            // creating Admin's own ClientId inherited automatically) - see the .proto comment.
            ClientId = ProtoConverters.ToNullableGuid(request.ClientId)
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync(context.CancellationToken);

        return ToSummary(user, role.Name);
    }

    [Authorize(Roles = "Admin,BaseRole")]
    public override async Task<UserSummary> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var id = ProtoConverters.ToGuid(request.Id);
        var user = await db.Users.Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"User '{request.Id}' was not found."));

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            ValidateLength(request.FullName, FullNameMaxLength, nameof(request.FullName));
            user.FullName = request.FullName.Trim();
        }
        user.IsActive = request.IsActive;
        user.TechnicianId = ProtoConverters.ToNullableGuid(request.TechnicianId);
        // Always the Gateway's final decided value - it re-sends the existing ClientId
        // unchanged when the caller isn't BaseRole, so this is safe to apply unconditionally.
        user.ClientId = ProtoConverters.ToNullableGuid(request.ClientId);

        string roleName;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, context.CancellationToken)
                ?? throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unknown role '{request.Role}'."));

            if (user.UserRole is null)
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            else
                user.UserRole.RoleId = role.Id;

            roleName = role.Name;
        }
        else
        {
            roleName = await db.UserRoles.Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role!.Name)
                .FirstOrDefaultAsync(context.CancellationToken) ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (request.NewPassword.Length < 8)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "New password must be at least 8 characters."));
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.NewPassword);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        return ToSummary(user, roleName);
    }

    /// <summary>Self-service - any authenticated user, but only ever against the id the
    /// Gateway derived from their own JWT (see the .proto comment on the service).</summary>
    public override async Task<UserSummary> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var id = ProtoConverters.ToGuid(request.Id);
        var user = await db.Users.AsNoTracking().Include(u => u.UserRole!).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"User '{request.Id}' was not found."));

        return ToSummary(user, user.UserRole?.Role?.Name ?? string.Empty);
    }

    /// <summary>Self-service update of the caller's own FullName/Email only - Role, IsActive and
    /// TechnicianId are admin-only concerns and deliberately not on this request at all (see
    /// UpdateUser for those).</summary>
    public override async Task<UserSummary> UpdateMyProfile(UpdateMyProfileRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Full name is required."));
        ValidateLength(request.FullName, FullNameMaxLength, nameof(request.FullName));
        ValidateLength(request.Email, EmailMaxLength, nameof(request.Email));

        var id = ProtoConverters.ToGuid(request.Id);
        var user = await db.Users.Include(u => u.UserRole!).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"User '{request.Id}' was not found."));

        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        return ToSummary(user, user.UserRole?.Role?.Name ?? string.Empty);
    }

    /// <summary>Self-service, and unlike UpdateUser's admin-set new_password, requires the
    /// caller's current password to verify it's really them typing, not just anyone with a
    /// still-valid session.</summary>
    public override async Task<UserSummary> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "New password must be at least 8 characters."));

        var id = ProtoConverters.ToGuid(request.Id);
        var user = await db.Users.Include(u => u.UserRole!).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"User '{request.Id}' was not found."));

        // InvalidArgument, not Unauthenticated - the Gateway maps Unauthenticated to a 401,
        // which the Blazor client treats as "your session is dead" and force-logs-out on. A
        // wrong current password is a bad request value, not a dead session.
        var hasher = new PasswordHasher<User>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Current password is incorrect."));

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        return ToSummary(user, user.UserRole?.Role?.Name ?? string.Empty);
    }

    // Mirrors UserContext's HasMaxLength calls - checked here too so an oversized field fails
    // with a clean 400 instead of reaching SaveChangesAsync and throwing an unhandled
    // DbUpdateException, which the Gateway can only surface as a bare 500 (same bug class fixed
    // in Ticket.API, Asset.API, Client.API and Assignment.API this round).
    private const int UsernameMaxLength = 64;
    private const int FullNameMaxLength = 128;
    private const int EmailMaxLength = 256;

    private static void ValidateLength(string? value, int maxLength, string fieldName)
    {
        if (value is { Length: var length } && length > maxLength)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"{fieldName} must not exceed {maxLength} characters (was {length})."));
        }
    }

    private static UserSummary ToSummary(User user, string roleName) => new()
    {
        Id = ProtoConverters.ToProto(user.Id),
        Username = user.Username,
        FullName = user.FullName,
        Role = roleName,
        IsActive = user.IsActive,
        TechnicianId = ProtoConverters.ToProto(user.TechnicianId),
        Email = user.Email ?? string.Empty,
        ClientId = ProtoConverters.ToProto(user.ClientId)
    };
}
