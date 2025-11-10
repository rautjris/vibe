using System.Globalization;
using Microsoft.AspNetCore.Components;
using MySpeaker.Models;
using MySpeaker.Services;

namespace MySpeaker.Components.Pages
{
    public partial class Home
    {
        private PlayerStatus? _status;
        private IReadOnlyList<StreamInfo> _streams = Array.Empty<StreamInfo>();
        private StreamInfo _draft = new();
        private bool _isBusy;
        private int _volume = 40;
        private string? _errorMessage;
        private string? _successMessage;
        private string _quickStreamUrl = string.Empty;
        private string? _selectedSource;
        private string? _selectedLoop;

        private static readonly (string Key, string Label)[] InputSources =
        {
        ("wifi", "Wi-Fi Streaming"),
        ("line-in", "Line-In"),
        ("bluetooth", "Bluetooth"),
        ("optical", "Optical"),
        ("co-axial", "Coaxial"),
        ("line-in2", "Line-In 2"),
        ("udisk", "USB Disk"),
        ("PCUSB", "USB DAC"),
    };

        private static readonly (int Value, string Label)[] LoopModes =
        {
        (0, "Repeat All"),
        (1, "Repeat One"),
        (2, "Shuffle + Repeat"),
        (3, "Shuffle"),
        (4, "No Repeat"),
        (5, "Shuffle + Repeat One"),
    };

        private string _formActionLabel => _isEditing ? "Update Stream" : "Save Stream";

        private bool _isEditing => _streams.Any(s => s.Id == _draft.Id);

        protected override async Task OnInitializedAsync()
        {
            await LoadStreamsAsync();
            await RefreshStatusAsync(true);
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
            if (showBusy)
            {
                _isBusy = true;
            }

            try
            {
                _errorMessage = silent ? _errorMessage : null;
                var status = await Speaker.GetStatusAsync();
                _status = status;
                if (status is not null)
                {
                    _volume = status.Volume;
                    _selectedLoop = status.LoopCode;
                }
                else if (!silent)
                {
                    _errorMessage = "Speaker status is unavailable.";
                }
            }
            finally
            {
                if (showBusy)
                {
                    _isBusy = false;
                }
            }
        }

        private async Task SendSpeakerCommand(Func<Task<bool>> callback, string successMessage)
        {
            if (_isBusy)
            {
                return;
            }

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

        private Task OnSourceChangedAsync(string value)
        {
            _selectedSource = value;
            if (string.IsNullOrWhiteSpace(_selectedSource))
            {
                return Task.CompletedTask;
            }

            return SendSpeakerCommand(() => Speaker.SwitchModeAsync(_selectedSource), $"Source switched to {_selectedSource}");
        }

        private async Task OnLoopChangedAsync(string value)
        {
            _selectedLoop = value;
            if (string.IsNullOrWhiteSpace(_selectedLoop))
            {
                return;
            }

            if (int.TryParse(_selectedLoop, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mode))
            {
                await SendSpeakerCommand(() => Speaker.SetLoopModeAsync(mode), "Loop mode updated");
            }
        }

        private async Task PlayQuickUrl()
        {
            if (string.IsNullOrWhiteSpace(_quickStreamUrl))
            {
                return;
            }

            _quickStreamUrl = _quickStreamUrl.Trim();
            await SendSpeakerCommand(() => Speaker.PlayUrlAsync(_quickStreamUrl), "Stream started");
        }

        private async Task PlaySavedStream(StreamInfo stream)
        {
            await SendSpeakerCommand(() => Speaker.PlayUrlAsync(stream.Url), $"Playing {stream.Name}");
        }

        private Task PlayPreviousAsync()
            => SendSpeakerCommand(() => Speaker.ControlAsync("prev"), "Previous track");

        private Task PauseAsync()
            => SendSpeakerCommand(() => Speaker.ControlAsync("pause"), "Paused");

        private Task ResumeAsync()
            => SendSpeakerCommand(() => Speaker.ControlAsync("resume"), "Playback resumed");

        private Task StopAsync()
            => SendSpeakerCommand(() => Speaker.ControlAsync("stop"), "Playback stopped");

        private Task PlayNextAsync()
            => SendSpeakerCommand(() => Speaker.ControlAsync("next"), "Next track");

        private Task VolumeDownAsync()
            => SendSpeakerCommand(() => Speaker.AdjustVolumeAsync(false), "Volume decreased");

        private Task VolumeUpAsync()
            => SendSpeakerCommand(() => Speaker.AdjustVolumeAsync(true), "Volume increased");

        private Task ToggleMuteAsync()
        {
            var target = !(_status?.IsMuted ?? false);
            var message = target ? "Muted" : "Unmuted";
            return SendSpeakerCommand(() => Speaker.SetMuteAsync(target), message);
        }

        private async Task DeleteStreamAsync(StreamInfo stream)
        {
            _errorMessage = null;
            _successMessage = null;
            try
            {
                _streams = await StreamStore.DeleteAsync(stream.Id);
                _successMessage = $"Removed {stream.Name}.";
                if (_draft.Id == stream.Id)
                {
                    _draft = new StreamInfo();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete stream {Stream}", stream.Name);
                _errorMessage = "Unable to remove stream.";
            }
        }

        private void BeginEditStream(StreamInfo stream)
        {
            _errorMessage = null;
            _successMessage = null;
            _draft = stream.Clone();
        }

        private void BeginAddStream()
        {
            _errorMessage = null;
            _successMessage = null;
            _draft = new StreamInfo();
        }

        private async Task SaveStreamAsync()
        {
            _errorMessage = null;
            _successMessage = null;
            try
            {
                _streams = await StreamStore.UpsertAsync(_draft);
                _successMessage = _isEditing ? "Stream updated." : "Stream saved.";
                _draft = new StreamInfo();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save stream");
                _errorMessage = ex.Message;
            }
        }
    }
}