using System;
using System.Threading;
using System.Threading.Tasks;
using MySpeaker.Models;

namespace MySpeaker.Services;

public interface ISpeakerApi
{
    Task<PlayerStatus?> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<bool> SwitchModeAsync(string playerMode, CancellationToken cancellationToken = default);

    Task<bool> PlayUrlAsync(string url, CancellationToken cancellationToken = default);

    Task<bool> PlayPlaylistAsync(string url, CancellationToken cancellationToken = default);

    Task<bool> PlayIndexAsync(int index, CancellationToken cancellationToken = default);

    Task<bool> SetLoopModeAsync(int mode, CancellationToken cancellationToken = default);

    Task<bool> ControlAsync(string control, CancellationToken cancellationToken = default);

    Task<bool> SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    Task<bool> SetVolumeAsync(int volume, CancellationToken cancellationToken = default);

    Task<bool> AdjustVolumeAsync(bool increase, CancellationToken cancellationToken = default);

    Task<bool> SetMuteAsync(bool mute, CancellationToken cancellationToken = default);
}
