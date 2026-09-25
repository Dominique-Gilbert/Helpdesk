using Helpdesk.Tickets.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Tickets.Infrastructure;

public class TicketContext(DbContextOptions<TicketContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Reference).HasMaxLength(32).IsRequired();
            entity.HasIndex(t => t.Reference).IsUnique();
            entity.Property(t => t.Title).HasMaxLength(256).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(4000);
            entity.Property(t => t.RequestedBy).HasMaxLength(128);
            entity.HasIndex(t => t.RequestedByUserId);
            entity.Property(t => t.RequestedByUsername).HasMaxLength(64);
            entity.Property(t => t.RequestedByRole).HasMaxLength(32);

            // Stored as text, not int: a migration that renumbers an enum should not
            // silently reinterpret existing rows.
            entity.Property(t => t.Category).HasConversion<string>().HasMaxLength(32);
            entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(32);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasIndex(t => t.Status);
            entity.HasMany(t => t.Comments)
                  .WithOne(c => c.Ticket!)
                  .HasForeignKey(c => c.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Author).HasMaxLength(128).IsRequired();
            entity.Property(c => c.Body).HasMaxLength(4000).IsRequired();
            entity.HasIndex(c => c.TicketId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
