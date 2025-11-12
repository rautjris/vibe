using Microsoft.AspNetCore.Components;
using MySpeaker.Models;
using MySpeaker.Services;

namespace MySpeaker.Components.Pages
{
    public partial class Home
    {
        private PlayerStatus? _status;
        private IReadOnlyList<StreamInfo> _streams = Array.Empty<StreamInfo>();
        private bool _isBusy;
        private int _volume = 40;
        private string? _errorMessage;
        private string? _successMessage;

        [Inject] private PlayerStatusCache StatusCache { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await LoadStreamsAsync();
            UpdateStatusFromCache();
        }

        protected override void OnParametersSet()
        {
            UpdateStatusFromCache();
        }

        private void UpdateStatusFromCache()
        {
            _status = StatusCache.Current;
            if (_status is not null)
            {
                _volume = _status.Volume;
            }
        }

        private async Task LoadStreamsAsync()
        {
            try
            {
                _streams = await StreamStore.GetAllAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load streams");
                _errorMessage = "Unable to load saved streams.";
            }
        }

        private async Task RefreshStatusAsync(bool silent, bool showBusy = false)
        {
            if (showBusy) _isBusy = true;
            try
            {
                _errorMessage = silent ? _errorMessage : null;
                UpdateStatusFromCache();
                if (_status is null && !silent)
                {
                    _errorMessage = "Speaker status is unavailable.";
                }
            }
            finally
            {
                if (showBusy) _isBusy = false;
            }
        }

        private async Task SendSpeakerCommand(Func<Task<bool>> callback, string successMessage)
        {
            if (_isBusy) return;
            _isBusy = true;
            _errorMessage = null;
            _successMessage = null;
            try
            {
                var ok = await callback();
                if (ok)
                {
                    _successMessage = successMessage;
                    await RefreshStatusAsync(true);
                }
                else
                {
                    _errorMessage = "Speaker did not acknowledge the command.";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Speaker command failed");
                _errorMessage = "Command failed. Check the speaker connection.";
            }
            finally
            {
                _isBusy = false;
            }
        }

        private Task OnVolumeChangedAsync(int value)
        {
            _volume = value;
            return SendSpeakerCommand(() => Speaker.SetVolumeAsync(_volume), $"Volume set to {_volume}");
        }

        private async Task PlaySavedStream(StreamInfo stream)
        {
            if (!IsAllowedUrl(stream.Url))
            {
                _errorMessage = "Saved stream has unsupported URL scheme.";
                return;
            }
            await SendSpeakerCommand(() => Speaker.PlayUrlAsync(stream.Url), $"Playing {stream.Name}");
        }

        private static bool IsAllowedUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme is "http" or "https";
        }

        private Task StopAsync() => SendSpeakerCommand(() => Speaker.ControlAsync("stop"), "Playback stopped");
        private Task VolumeDownAsync() => SendSpeakerCommand(() => Speaker.AdjustVolumeAsync(false), "Volume decreased");
        private Task VolumeUpAsync() => SendSpeakerCommand(() => Speaker.AdjustVolumeAsync(true), "Volume increased");
        private Task ToggleMuteAsync()
        {
            var target = !(_status?.IsMuted ?? false);
            var message = target ? "Muted" : "Unmuted";
            return SendSpeakerCommand(() => Speaker.SetMuteAsync(target), message);
        }
    }
}