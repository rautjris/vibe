using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySpeaker.Models;

namespace MySpeaker.Services;

public sealed class MockSpeakerApi : ISpeakerApi
{
    private readonly ILogger<MockSpeakerApi> _logger;
    private readonly object _gate = new();
    private MockState _state = MockState.CreateDefault();

    public MockSpeakerApi(ILogger<MockSpeakerApi> logger)
    {
        _logger = logger;
    }

    public Task<PlayerStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<PlayerStatus?>(_state.ToStatus());
        }
    }

    public Task<bool> SwitchModeAsync(string playerMode, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (MockState.TryGetModeCode(playerMode, out var code))
            {
                _state = _state with { ModeCode = code };
                _logger.LogInformation("Mock switched mode to {Mode}", playerMode);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Mock received unknown input source {Mode}", playerMode);
            return Task.FromResult(false);
        }
    }

    public Task<bool> PlayUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var trackName = MockState.ExtractName(url);
            _state = _state with
            {
                Title = trackName,
                Artist = "Mock Artist",
                Album = "Mock Album",
                RawStatus = "play",
                CurrentPosition = TimeSpan.Zero,
                TotalLength = TimeSpan.FromMinutes(5),
                LastStreamUrl = url,
            };

            _logger.LogInformation("Mock started playing URL {Url}", url);
            return Task.FromResult(true);
        }
    }

    public Task<bool> PlayPlaylistAsync(string url, CancellationToken cancellationToken = default)
        => PlayUrlAsync(url, cancellationToken);

    public Task<bool> PlayIndexAsync(int index, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var newIndex = Math.Max(0, index - 1);
            _state = _state with { PlaylistIndex = newIndex, RawStatus = "play" };
            _logger.LogInformation("Mock switched to playlist index {Index}", newIndex);
            return Task.FromResult(true);
        }
    }

    public Task<bool> SetLoopModeAsync(int mode, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var loopCode = mode.ToString();
            _state = _state with { LoopCode = loopCode };
            _logger.LogInformation("Mock loop mode set to {Mode}", mode);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ControlAsync(string control, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            switch (control)
            {
                case "pause":
                    _state = _state with { RawStatus = "pause" };
                    break;
                case "resume":
                    _state = _state with { RawStatus = "play" };
                    break;
                case "onepause":
                    _state = _state with { RawStatus = _state.RawStatus == "play" ? "pause" : "play" };
                    break;
                case "stop":
                    _state = _state with { RawStatus = "stop", CurrentPosition = TimeSpan.Zero };
                    break;
                case "prev":
                    var previousIndex = Math.Max(0, _state.PlaylistIndex - 1);
                    _state = _state with { PlaylistIndex = previousIndex, RawStatus = "play" };
                    break;
                case "next":
                    var maxIndex = Math.Max(0, _state.PlaylistCount - 1);
                    var nextIndex = Math.Min(maxIndex, _state.PlaylistIndex + 1);
                    _state = _state with { PlaylistIndex = nextIndex, RawStatus = "play" };
                    break;
                default:
                    _logger.LogWarning("Mock received unsupported control {Control}", control);
                    return Task.FromResult(false);
            }

            _logger.LogInformation("Mock control executed: {Control}", control);
            return Task.FromResult(true);
        }
    }

    public Task<bool> SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var clamped = _state.TotalLength.HasValue
                ? TimeSpan.FromMilliseconds(Math.Clamp(position.TotalMilliseconds, 0, _state.TotalLength.Value.TotalMilliseconds))
                : position < TimeSpan.Zero ? TimeSpan.Zero : position;

            _state = _state with { CurrentPosition = clamped };
            _logger.LogInformation("Mock seek performed: {Position}", clamped);
            return Task.FromResult(true);
        }
    }

    public Task<bool> SetVolumeAsync(int volume, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var clamped = Math.Clamp(volume, 0, 100);
            _state = _state with { Volume = clamped };
            _logger.LogInformation("Mock volume set to {Volume}", clamped);
            return Task.FromResult(true);
        }
    }

    public Task<bool> AdjustVolumeAsync(bool increase, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var delta = increase ? 6 : -6;
            var target = Math.Clamp(_state.Volume + delta, 0, 100);
            _state = _state with { Volume = target };
            _logger.LogInformation("Mock volume adjusted by {Delta} to {Volume}", delta, target);
            return Task.FromResult(true);
        }
    }

    public Task<bool> SetMuteAsync(bool mute, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _state = _state with { IsMuted = mute };
            _logger.LogInformation("Mock mute toggled to {Mute}", mute);
            return Task.FromResult(true);
        }
    }

    private sealed record MockState(
        string ModeCode,
        string LoopCode,
        string RawStatus,
        int PlaylistCount,
        int PlaylistIndex,
        TimeSpan CurrentPosition,
        TimeSpan? TotalLength,
        int Volume,
        bool IsMuted,
        string Title,
        string Artist,
        string Album,
        string? LastStreamUrl)
    {
        public PlayerStatus ToStatus()
        {
            return new PlayerStatus(
                deviceTypeCode: "0",
                channelCode: "0",
                modeCode: ModeCode,
                loopCode: LoopCode,
                equalizer: 0,
                rawStatus: RawStatus,
                currentPosition: CurrentPosition,
                playlistOffset: CurrentPosition,
                totalLength: TotalLength,
                playlistCount: PlaylistCount,
                playlistIndex: PlaylistIndex,
                volume: Volume,
                isMuted: IsMuted,
                title: Title,
                artist: Artist,
                album: Album);
        }

        public static MockState CreateDefault()
        {
            return new MockState(
                ModeCode: "10",
                LoopCode: "4",
                RawStatus: "stop",
                PlaylistCount: 5,
                PlaylistIndex: 0,
                CurrentPosition: TimeSpan.Zero,
                TotalLength: TimeSpan.FromMinutes(5),
                Volume: 42,
                IsMuted: false,
                Title: "Welcome Track",
                Artist: "Mock Artist",
                Album: "Mock Album",
                LastStreamUrl: null);
        }

        public static bool TryGetModeCode(string playerMode, out string code)
        {
            if (string.IsNullOrWhiteSpace(playerMode))
            {
                code = "0";
                return false;
            }

            var trimmed = playerMode.Trim();
            if (ModeMappings.TryGetValue(trimmed, out var mapped))
            {
                code = mapped;
                return true;
            }

            code = "0";
            return false;
        }

        public static string ExtractName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "Stream";
            }

            try
            {
                var uri = new Uri(url, UriKind.RelativeOrAbsolute);
                if (uri.IsAbsoluteUri)
                {
                    var segments = uri.Segments;
                    if (segments.Length > 0)
                    {
                        var segment = segments[^1].Trim('/');
                        if (!string.IsNullOrEmpty(segment))
                        {
                            return segment;
                        }
                    }

                    return uri.Host;
                }

                return url;
            }
            catch (UriFormatException)
            {
                return url;
            }
        }

        private static readonly Dictionary<string, string> ModeMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["wifi"] = "10",
            ["line-in"] = "40",
            ["bluetooth"] = "41",
            ["optical"] = "43",
            ["co-axial"] = "47",
            ["coaxial"] = "47",
            ["line-in2"] = "47",
            ["udisk"] = "11",
            ["pcusb"] = "51",
            ["pc-usb"] = "51",
        };
    }
}
