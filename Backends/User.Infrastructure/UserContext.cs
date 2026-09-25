using Helpdesk.Users.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Users.Infrastructure;

public class UserContext(DbContextOptions<UserContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).HasMaxLength(64).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(128).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(32).IsRequired();
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => ur.Id);

            // One role per user, enforced by the database, not just by seeding discipline.
            entity.HasIndex(ur => ur.UserId).IsUnique();

            entity.HasOne(ur => ur.User)
                  .WithOne(u => u.UserRole)
                  .HasForeignKey<UserRole>(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany()
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
