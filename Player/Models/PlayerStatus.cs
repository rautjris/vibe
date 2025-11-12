using System;
using System.Collections.Generic;
using System.Globalization;

namespace MySpeaker.Models;

public enum PlayerMode
{
    Unknown,
    Idle,
    AirPlay,
    Dlna,
    NetworkStream,
    UsbDisk,
    HttpApi,
    SpotifyConnect,
    LineIn,
    Bluetooth,
    Optical,
    LineIn2,
    UsbDac,
    MultiroomGuest
}

public enum LoopMode
{
    Unknown,
    RepeatAll,
    RepeatOne,
    ShuffleRepeat,
    Shuffle,
    NoRepeat,
    ShuffleRepeatOne
}

public sealed class PlayerStatus
{
    private static readonly Dictionary<string, PlayerMode> ModeMap = new()
    {
        ["0"] = PlayerMode.Idle,
        ["1"] = PlayerMode.AirPlay,
        ["2"] = PlayerMode.Dlna,
        ["10"] = PlayerMode.NetworkStream,
        ["11"] = PlayerMode.UsbDisk,
        ["20"] = PlayerMode.HttpApi,
        ["31"] = PlayerMode.SpotifyConnect,
        ["40"] = PlayerMode.LineIn,
        ["41"] = PlayerMode.Bluetooth,
        ["43"] = PlayerMode.Optical,
        ["47"] = PlayerMode.LineIn2,
        ["51"] = PlayerMode.UsbDac,
        ["99"] = PlayerMode.MultiroomGuest,
    };

    private static readonly Dictionary<string, LoopMode> LoopMap = new()
    {
        ["0"] = LoopMode.RepeatAll,
        ["1"] = LoopMode.RepeatOne,
        ["2"] = LoopMode.ShuffleRepeat,
        ["3"] = LoopMode.Shuffle,
        ["4"] = LoopMode.NoRepeat,
        ["5"] = LoopMode.ShuffleRepeatOne,
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
        Mode = ModeMap.TryGetValue(ModeCode, out var pm) ? pm : PlayerMode.Unknown;
        Loop = LoopMap.TryGetValue(LoopCode, out var lm) ? lm : LoopMode.Unknown;
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

        StatusLabel = string.IsNullOrWhiteSpace(RawStatus)
            ? "Unknown"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(RawStatus);

        PositionDisplay = BuildPositionDisplay(CurrentPosition, TotalLength);
        PlaylistDisplay = BuildPlaylistDisplay(PlaylistCount, PlaylistIndex);
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
    public PlayerMode Mode { get; }
    public string LoopCode { get; }
    public LoopMode Loop { get; }

    public int? Equalizer { get; }
    public string RawStatus { get; }
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

    public string StatusLabel { get; }
    public string PositionDisplay { get; }
    public string PlaylistDisplay { get; }

    private static string BuildPositionDisplay(TimeSpan? position, TimeSpan? length)
    {
        if (position is null && length is null) return "N/A";
        var pos = position ?? TimeSpan.Zero;
        return length.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "{0:mm\\:ss} / {1:mm\\:ss}", pos, length.Value)
            : string.Format(CultureInfo.InvariantCulture, "{0:mm\\:ss}", pos);
    }

    private static string BuildPlaylistDisplay(int? count, int? index)
    {
        if (count is > 0 && index is not null)
        {
            var current = Math.Clamp(index.Value + 1, 1, count.Value);
            return string.Format(CultureInfo.InvariantCulture, "Track {0} of {1}", current, count.Value);
        }
        return "N/A";
    }
}
