using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helpdesk.Persistence;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies migrations at startup in a manual scope, the Supercard way.
    ///
    /// Compose already gates on the database healthcheck, but "postgres accepts TCP" and
    /// "postgres will accept my DDL" are not the same instant, so this retries rather than
    /// crash-looping the container on the first cold start.
    /// </summary>
    public static async Task MigrateDatabaseAsync<TContext>(this IHost host, int attempts = 10)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Helpdesk.Persistence.Migration");

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

                if (pending.Count == 0)
                {
                    await context.Database.EnsureCreatedAsync();
                    logger.LogInformation("{Context} database ensured without pending migrations", typeof(TContext).Name);
                    return;
                }

                await context.Database.MigrateAsync();
                logger.LogInformation("{Context} migrations applied", typeof(TContext).Name);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                logger.LogWarning(ex, "Migration attempt {Attempt}/{Attempts} for {Context} failed; retrying",
                    attempt, attempts, typeof(TContext).Name);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        // Final attempt outside the catch: if this throws, the service should not start.
        var finalPending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (finalPending.Count == 0)
        {
            await context.Database.EnsureCreatedAsync();
            return;
        }

        await context.Database.MigrateAsync();
    }
}
