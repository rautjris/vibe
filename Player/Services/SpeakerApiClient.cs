using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySpeaker.Models;

namespace MySpeaker.Services;

public sealed class SpeakerApiClient : ISpeakerApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<SpeakerApiClient> _logger;

    public SpeakerApiClient(HttpClient httpClient, ILogger<SpeakerApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlayerStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("httpapi.asp?command=getPlayerStatus", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Speaker status request failed with {StatusCode}", response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<PlayerStatusPayload>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            return payload?.ToModel();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Unable to retrieve player status.");
            return null;
        }
    }

    public Task<bool> SwitchModeAsync(string playerMode, CancellationToken cancellationToken = default)
        => SendCommandAsync($"setPlayerCmd:switchmode:{playerMode}", cancellationToken);

    public Task<bool> PlayUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(url);
        return SendCommandAsync($"setPlayerCmd:play:{encoded}", cancellationToken);
    }

    public Task<bool> PlayPlaylistAsync(string url, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(url);
        return SendCommandAsync($"setPlayerCmd:m3u:play:{encoded}", cancellationToken);
    }

    public Task<bool> PlayIndexAsync(int index, CancellationToken cancellationToken = default)
        => SendCommandAsync($"setPlayerCmd:playindex:{index}", cancellationToken);

    public Task<bool> SetLoopModeAsync(int mode, CancellationToken cancellationToken = default)
        => SendCommandAsync($"setPlayerCmd:loopmode:{mode}", cancellationToken);

    public Task<bool> ControlAsync(string control, CancellationToken cancellationToken = default)
        => SendCommandAsync($"setPlayerCmd:{control}", cancellationToken);

    public Task<bool> SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        var seconds = (int)Math.Max(0, Math.Round(position.TotalSeconds));
        return SendCommandAsync($"setPlayerCmd:seek:{seconds}", cancellationToken);
    }

    public Task<bool> SetVolumeAsync(int volume, CancellationToken cancellationToken = default)
    {
        var clamped = Math.Clamp(volume, 0, 100);
        return SendCommandAsync($"setPlayerCmd:vol:{clamped}", cancellationToken);
    }

    public Task<bool> AdjustVolumeAsync(bool increase, CancellationToken cancellationToken = default)
        => SendCommandAsync(increase ? "setPlayerCmd:vol%2b%2b" : "setPlayerCmd:vol--", cancellationToken);

    public Task<bool> SetMuteAsync(bool mute, CancellationToken cancellationToken = default)
        => SendCommandAsync($"setPlayerCmd:mute:{(mute ? 1 : 0)}", cancellationToken);

    private async Task<bool> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"httpapi.asp?command={command}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Speaker command {Command} failed with {StatusCode}", command, response.StatusCode);
                return false;
            }

            var payload = (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
            return string.Equals(payload, "OK", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Command {Command} failed", command);
            return false;
        }
    }

    private sealed record PlayerStatusPayload(
        string? Type,
        string? Ch,
        string? Mode,
        string? Loop,
        string? Eq,
        string? Status,
        string? Curpos,
        string? Offset_Pts,
        string? Totlen,
        string? Title,
        string? Artist,
        string? Album,
        string? Alarmflag,
        string? Plicount,
        string? Plicurr,
        string? Vol,
        string? Mute)
    {
        public PlayerStatus ToModel()
        {
            return new PlayerStatus(
                Type,
                Ch,
                Mode,
                Loop,
                ParseInt(Eq),
                Status,
                ParseMillis(Curpos),
                ParseMillis(Offset_Pts),
                ParseMillis(Totlen),
                ParseInt(Plicount),
                ParseInt(Plicurr),
                ParseInt(Vol),
                ParseBool(Mute),
                DecodeHex(Title),
                DecodeHex(Artist),
                DecodeHex(Album));
        }

        private static int? ParseInt(string? value)
            => int.TryParse(value, out var result) ? result : null;

        private static bool? ParseBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim() == "1";
        }

        private static TimeSpan? ParseMillis(string? value)
        {
            if (!int.TryParse(value, out var millis)) return null;
            return TimeSpan.FromMilliseconds(Math.Max(0, millis));
        }

        private static string? DecodeHex(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                var bytes = Convert.FromHexString(value);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return value;
            }
        }
    }
}
