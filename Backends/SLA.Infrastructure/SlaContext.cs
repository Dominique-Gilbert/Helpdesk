using Helpdesk.Sla.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Sla.Infrastructure;

public class SlaContext(DbContextOptions<SlaContext> options) : DbContext(options)
{
    public DbSet<SlaClock> SlaClocks => Set<SlaClock>();
    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<SlaAdjustment> Adjustments => Set<SlaAdjustment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SlaClock>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.TicketReference).HasMaxLength(32);
            entity.Property(c => c.Priority).HasConversion<string>().HasMaxLength(32);
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);

            // One clock per ticket - the consumer is idempotent, and so is the schema.
            entity.HasIndex(c => c.TicketId).IsUnique();
            // The breach monitor scans on these two columns every tick.
            entity.HasIndex(c => new { c.Status, c.DueAtUtc });

            entity.HasMany(c => c.Escalations)
                  .WithOne(e => e.SlaClock!)
                  .HasForeignKey(e => e.SlaClockId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Adjustments)
                  .WithOne(a => a.SlaClock!)
                  .HasForeignKey(a => a.SlaClockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Escalation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(512);
            entity.HasIndex(e => new { e.SlaClockId, e.Level }).IsUnique();
        });

        modelBuilder.Entity<SlaAdjustment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Reason).HasMaxLength(512).IsRequired();
            entity.Property(a => a.AdjustedBy).HasMaxLength(128);
            entity.HasIndex(a => a.SlaClockId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
