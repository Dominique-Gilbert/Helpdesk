using Helpdesk.Assets.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Assets.Infrastructure;

public class AssetContext(DbContextOptions<AssetContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssignmentRecord> AssignmentRecords => Set<AssignmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Tag).HasMaxLength(32).IsRequired();
            entity.HasIndex(a => a.Tag).IsUnique();
            entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
            entity.Property(a => a.SerialNumber).HasMaxLength(64);
            entity.Property(a => a.Location).HasMaxLength(128);
            entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasMany(a => a.AssignmentRecords)
                  .WithOne(r => r.Asset!)
                  .HasForeignKey(r => r.AssetId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssignmentRecord>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.AssignedTo).HasMaxLength(128);
            entity.Property(r => r.Notes).HasMaxLength(512);
            entity.Ignore(r => r.IsOpen);
            entity.HasIndex(r => r.TicketId);
            entity.HasIndex(r => new { r.AssetId, r.ReturnedAtUtc });
        });

        base.OnModelCreating(modelBuilder);
    }
}
