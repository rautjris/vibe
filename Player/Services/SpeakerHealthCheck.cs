using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySpeaker.Services;

namespace MySpeaker.Services;

public sealed class SpeakerHealthCheck : IHealthCheck
{
    private readonly ISpeakerApi _api;

    public SpeakerHealthCheck(ISpeakerApi api)
    {
        _api = api;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = await _api.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return status is not null
            ? HealthCheckResult.Healthy("Speaker reachable")
            : HealthCheckResult.Degraded("Speaker unreachable or returned null status");
    }
}
