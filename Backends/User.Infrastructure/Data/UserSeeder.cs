using Helpdesk.Users.Domain.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Users.Infrastructure.Data;

/// <summary>
/// This is a Help Desk, not a public product - accounts are created by an admin, not signed up.
/// Until that admin-provisioning flow exists, seed a couple of demo accounts so login has
/// something to authenticate against.
///
/// Seeded per-item (by role name / username), not gated behind a single "any users?" check -
/// that way adding a new role or demo account later (e.g. Support) still lands on a database
/// that was already seeded before that role existed, instead of silently never appearing.
/// </summary>
public static class UserSeeder
{
    // Assignment.API's Technicians table is a different database - there's no real foreign
    // key across it, so this has to be a fixed, known value matching the equally-fixed Id
    // AssignmentSeeder.cs gives Ayanda Nkosi. If either seeder ever generates a random Guid
    // instead, the two sides drift apart silently.
    private static readonly Guid AyandaNkosiTechnicianId = Guid.Parse("D176B3A9-2C03-4062-BD95-18DA6155AA47");

    public static async Task SeedAsync(UserContext context, CancellationToken ct = default)
    {
        var adminRole = await GetOrAddRoleAsync(context, "Admin", ct);
        // BaseRole currently carries the exact same permissions as Admin everywhere they're
        // checked ([Authorize(Roles = "Admin,BaseRole")], the scope.Role comparisons in the
        // Gateway's TicketService/SlaService, and the frontend's Admin-gated pages) - it's a
        // stopgap until real per-permission roles exist, not a distinct permission set.
        var baseRole = await GetOrAddRoleAsync(context, "BaseRole", ct);
        var technicianRole = await GetOrAddRoleAsync(context, "Technician", ct);
        var supportRole = await GetOrAddRoleAsync(context, "Support", ct);
        await context.SaveChangesAsync(ct);

        var hasher = new PasswordHasher<User>();

        await GetOrAddUserAsync(context, "admin", adminRole, hasher, ct,
            configure: u => u.FullName = "Helpdesk Administrator");

        await GetOrAddUserAsync(context, "baserole", baseRole, hasher, ct,
            configure: u => u.FullName = "Base Role Demo");

        // FullName is just a display name now - TechnicianId is the actual link "My Tickets"
        // filters on.
        await GetOrAddUserAsync(context, "technician", technicianRole, hasher, ct,
            configure: u =>
            {
                u.FullName = "Ayanda Nkosi";
                u.TechnicianId = AyandaNkosiTechnicianId;
            });

        await GetOrAddUserAsync(context, "support", supportRole, hasher, ct,
            configure: u => u.FullName = "Priya Naidoo");

        await context.SaveChangesAsync(ct);
    }

    private static async Task<Role> GetOrAddRoleAsync(UserContext context, string name, CancellationToken ct)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
        if (role is not null) return role;

        role = new Role { Name = name };
        context.Roles.Add(role);
        return role;
    }

    private static async Task GetOrAddUserAsync(
        UserContext context, string username, Role role, PasswordHasher<User> hasher, CancellationToken ct,
        Action<User> configure)
    {
        if (await context.Users.AnyAsync(u => u.Username == username, ct)) return;

        var user = new User { Username = username };
        configure(user);
        user.PasswordHash = hasher.HashPassword(user, "Passw0rd!");

        context.Users.Add(user);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
    }
}
