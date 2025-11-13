using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MySpeaker.Services;

public sealed class StreamStoreHealthCheck : IHealthCheck
{
    private readonly StreamStore _store;

    public StreamStoreHealthCheck(StreamStore store)
    {
        _store = store;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _store.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy($"{items.Count} streams loaded");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to read stream store", ex);
        }
    }
}
