using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MySpeaker.Services;

public sealed class PlayerStatusPollingService : BackgroundService
{
    private readonly ISpeakerApi _speakerApi;
    private readonly PlayerStatusCache _cache;
    private readonly ILogger<PlayerStatusPollingService> _logger;

    public PlayerStatusPollingService(ISpeakerApi speakerApi, PlayerStatusCache cache, ILogger<PlayerStatusPollingService> logger)
    {
        _speakerApi = speakerApi;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlayerStatus polling service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = await _speakerApi.GetStatusAsync(stoppingToken).ConfigureAwait(false);
                _cache.Set(status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Status polling failed");
            }

            // Poll every 2 seconds
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
        }
        _logger.LogInformation("PlayerStatus polling service stopped");
    }
}
