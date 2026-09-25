using Helpdesk.Clients.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Clients.Infrastructure;

public class ClientContext(DbContextOptions<ClientContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Company).HasMaxLength(128).IsRequired();
            entity.Property(c => c.Name).HasMaxLength(128).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(256);
            entity.Property(c => c.ContactNumber).HasMaxLength(32);
            entity.Property(c => c.Description).HasMaxLength(512);
            entity.Property(c => c.AboutInfo).HasMaxLength(4000);
            entity.Property(c => c.PrimaryColor).HasMaxLength(9);
            entity.Property(c => c.LogoUrl).HasMaxLength(2048);
        });

        base.OnModelCreating(modelBuilder);
    }
}
