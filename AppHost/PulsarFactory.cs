using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>
/// A single Pulsar standalone broker plus a one-shot init container that provisions the
/// "helpdesk" tenant/namespace and the topics this app uses (ticket-created, sla-breached,
/// ticket-closed, ticket-reopened). Both broker ports are published directly (isProxied:
/// false), so consuming
/// services can just point at pulsar://localhost:6650 - no service-discovery indirection
/// needed for a single-broker local dev setup.
/// </summary>
internal static class PulsarFactory
{
    public static (IResourceBuilder<ContainerResource> Broker, IResourceBuilder<ContainerResource> Init) PreparePulsar(
        IDistributedApplicationBuilder builder)
    {
        var broker = builder.AddContainer("pulsar", "apachepulsar/pulsar", "4.2.2")
            .WithEndpoint(port: 6650, targetPort: 6650, scheme: "tcp", name: "broker", isProxied: false)
            .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "admin", isProxied: false)
            .WithEnvironment("PULSAR_PREFIX_advertisedAddress", "localhost")
            .WithArgs("bin/pulsar", "standalone")
            .WithLifetime(ContainerLifetime.Persistent);

        // Idempotent (every pulsar-admin call is "|| true"), so it's safe to re-run this on
        // every AppHost start even though the tenant/namespace/topics already exist.
        var init = builder.AddContainer("pulsar-init", "apachepulsar/pulsar", "4.2.2")
            .WithEnvironment("PULSAR_ADMIN_URL", "http://pulsar:8080")
            .WithArgs(
                "sh",
                "-c",
                "echo 'Waiting for Pulsar...'; " +
                "COUNT=0; " +
                "until curl -sf \"$PULSAR_ADMIN_URL/admin/v2/clusters\" > /dev/null; do " +
                    "COUNT=$((COUNT+1)); " +
                    "if [ $COUNT -gt 90 ]; then echo 'Timeout waiting for Pulsar'; exit 1; fi; " +
                    "sleep 2; " +
                "done; " +
                "echo 'Pulsar is up'; " +
                "bin/pulsar-admin --admin-url \"$PULSAR_ADMIN_URL\" tenants create helpdesk --allowed-clusters standalone || true; " +
                "bin/pulsar-admin --admin-url \"$PULSAR_ADMIN_URL\" namespaces create helpdesk/events || true; " +
                "bin/pulsar-admin --admin-url \"$PULSAR_ADMIN_URL\" topics create persistent://helpdesk/events/ticket-created || true; " +
                "bin/pulsar-admin --admin-url \"$PULSAR_ADMIN_URL\" topics create persistent://helpdesk/events/sla-breached || true; " +
                "bin/pulsar-admin --admin-url \"$PULSAR_ADMIN_URL\" topics create persistent://helpdesk/events/ticket-closed || true; " +
                "bin/pulsar-admin --admin-url \"$PULSAR_ADMIN_URL\" topics create persistent://helpdesk/events/ticket-reopened || true; " +
                "echo 'Done';")
            .WaitFor(broker);

        return (broker, init);
    }
}
