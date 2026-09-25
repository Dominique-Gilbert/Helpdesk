using Helpdesk.Assignments.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assignments.Infrastructure;

public class AssignmentContext(DbContextOptions<AssignmentContext> options) : DbContext(options)
{
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<TicketAssignment> TicketAssignments => Set<TicketAssignment>();
    public DbSet<TechnicianCategoryLevel> TechnicianCategoryLevels => Set<TechnicianCategoryLevel>();
    public DbSet<AssignmentBacklogEntry> BacklogEntries => Set<AssignmentBacklogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Technician>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.FullName).HasMaxLength(128).IsRequired();
            entity.Property(t => t.Email).HasMaxLength(256);
            entity.Property(t => t.Team).HasMaxLength(64).IsRequired();
            entity.Ignore(t => t.HasCapacity);
            entity.HasIndex(t => t.Team);
        });

        modelBuilder.Entity<RoutingRule>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(128).IsRequired();
            entity.Property(r => r.Category).HasMaxLength(64);
            entity.Property(r => r.Team).HasMaxLength(64).IsRequired();
            entity.Property(r => r.MinPriority).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(r => r.Rank);
        });

        modelBuilder.Entity<TicketAssignment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.TicketReference).HasMaxLength(32);
            entity.Property(a => a.Category).HasMaxLength(64);
            entity.Property(a => a.Priority).HasConversion<string>().HasMaxLength(32);
            entity.Property(a => a.Explanation).HasMaxLength(512);

            // One assignment per ticket. The consumer is idempotent by choice; this makes
            // the database enforce it too, so a redelivered event cannot double-assign.
            entity.HasIndex(a => a.TicketId).IsUnique();

            entity.HasOne(a => a.Technician)
                  .WithMany()
                  .HasForeignKey(a => a.TechnicianId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.RoutingRule)
                  .WithMany()
                  .HasForeignKey(a => a.RoutingRuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TechnicianCategoryLevel>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Category).HasMaxLength(64).IsRequired();
            entity.Property(l => l.Level).HasConversion<string>().HasMaxLength(32);

            // One level per (technician, category) - a second CreateTechnician/UpdateTechnician
            // call for the same category replaces the row rather than adding a duplicate.
            entity.HasIndex(l => new { l.TechnicianId, l.Category }).IsUnique();

            entity.HasOne(l => l.Technician)
                  .WithMany(t => t.CategoryLevels)
                  .HasForeignKey(l => l.TechnicianId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssignmentBacklogEntry>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.TicketReference).HasMaxLength(32);
            entity.Property(b => b.Category).HasMaxLength(64).IsRequired();
            entity.Property(b => b.Priority).HasConversion<string>().HasMaxLength(32);
            entity.Property(b => b.StartingLevel).HasConversion<string>().HasMaxLength(32);
            entity.Property(b => b.CommentText).HasMaxLength(2000);
            entity.Property(b => b.RoutingPath).HasConversion<string>().HasMaxLength(16);

            // One backlog entry per ticket - same idempotency guarantee as TicketAssignment.
            entity.HasIndex(b => b.TicketId).IsUnique();
            entity.HasIndex(b => b.QueuedAtUtc);
        });

        base.OnModelCreating(modelBuilder);
    }
}
