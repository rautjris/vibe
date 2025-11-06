using System;
using System.Collections.Generic;
using System.Globalization;

namespace MySpeaker.Models;

public sealed class PlayerStatus
{
    private static readonly IReadOnlyDictionary<string, string> ModeDescriptions = new Dictionary<string, string>
    {
        ["0"] = "Idle",
        ["1"] = "AirPlay",
        ["2"] = "DLNA",
        ["10"] = "Network Stream",
        ["11"] = "USB Disk",
        ["20"] = "HTTP API",
        ["31"] = "Spotify Connect",
        ["40"] = "Line-In",
        ["41"] = "Bluetooth",
        ["43"] = "Optical",
        ["47"] = "Line-In 2",
        ["51"] = "USB DAC",
        ["99"] = "Multiroom Guest",
    };

    private static readonly IReadOnlyDictionary<string, string> LoopDescriptions = new Dictionary<string, string>
    {
        ["0"] = "Repeat All",
        ["1"] = "Repeat One",
        ["2"] = "Shuffle + Repeat",
        ["3"] = "Shuffle",
        ["4"] = "No Repeat",
        ["5"] = "Shuffle + Repeat One",
    };

    public PlayerStatus(
        string? deviceTypeCode,
        string? channelCode,
        string? modeCode,
        string? loopCode,
        int? equalizer,
        string? rawStatus,
        TimeSpan? currentPosition,
        TimeSpan? playlistOffset,
        TimeSpan? totalLength,
        int? playlistCount,
        int? playlistIndex,
        int? volume,
        bool? isMuted,
        string? title,
        string? artist,
        string? album)
    {
        DeviceTypeCode = deviceTypeCode ?? string.Empty;
        ChannelCode = channelCode ?? string.Empty;
        ModeCode = modeCode ?? string.Empty;
        LoopCode = loopCode ?? string.Empty;
        Equalizer = equalizer;
        RawStatus = rawStatus ?? string.Empty;
        CurrentPosition = currentPosition;
        PlaylistOffset = playlistOffset;
        TotalLength = totalLength;
        PlaylistCount = playlistCount;
        PlaylistIndex = playlistIndex;
        Volume = volume ?? 0;
        IsMuted = isMuted ?? false;
    Title = string.IsNullOrWhiteSpace(title) ? "(none)" : title;
    Artist = string.IsNullOrWhiteSpace(artist) ? "(none)" : artist;
    Album = string.IsNullOrWhiteSpace(album) ? "(none)" : album;
    }

    public string DeviceTypeCode { get; }

    public string DeviceType => DeviceTypeCode switch
    {
        "0" => "Standalone",
        "1" => "Multiroom Guest",
        _ => "Unknown",
    };

    public string ChannelCode { get; }

    public string Channel => ChannelCode switch
    {
        "0" => "Stereo",
        "1" => "Left",
        "2" => "Right",
        _ => "Unknown",
    };

    public string ModeCode { get; }

    public string Mode => ModeDescriptions.TryGetValue(ModeCode, out var label)
        ? label
        : string.IsNullOrEmpty(ModeCode) ? "Unknown" : $"Mode {ModeCode}";

    public string LoopCode { get; }

    public string LoopMode => LoopDescriptions.TryGetValue(LoopCode, out var label)
        ? label
        : string.IsNullOrEmpty(LoopCode) ? "Unknown" : $"Loop {LoopCode}";

    public int? Equalizer { get; }

    public string RawStatus { get; }

    public string StatusLabel => string.IsNullOrWhiteSpace(RawStatus)
        ? "Unknown"
        : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(RawStatus);

    public TimeSpan? CurrentPosition { get; }

    public TimeSpan? PlaylistOffset { get; }

    public TimeSpan? TotalLength { get; }

    public int? PlaylistCount { get; }

    public int? PlaylistIndex { get; }

    public int Volume { get; }

    public bool IsMuted { get; }

    public string Title { get; }

    public string Artist { get; }

    public string Album { get; }

    public string PositionDisplay
    {
        get
        {
            if (CurrentPosition is null && TotalLength is null)
            {
                return "N/A";
            }

            var position = CurrentPosition ?? TimeSpan.Zero;
            var length = TotalLength;
            return length.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "{0:mm\\:ss} / {1:mm\\:ss}", position, length.Value)
                : string.Format(CultureInfo.InvariantCulture, "{0:mm\\:ss}", position);
        }
    }

    public string PlaylistDisplay
    {
        get
        {
            if (PlaylistCount is > 0 && PlaylistIndex is not null)
            {
                var current = Math.Clamp(PlaylistIndex.Value + 1, 1, PlaylistCount.Value);
                return string.Format(CultureInfo.InvariantCulture, "Track {0} of {1}", current, PlaylistCount.Value);
            }

            return "N/A";
        }
    }
}
