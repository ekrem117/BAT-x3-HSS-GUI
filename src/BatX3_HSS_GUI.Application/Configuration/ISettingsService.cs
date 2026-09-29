using BatX3_HSS_GUI.Application.Configuration.Input;

namespace BatX3_HSS_GUI.Application.Configuration
{
    public interface ISettingsService
    {
        NetworkSettings GetNetworkSettings();

        InputSettings GetInputSettings();

        VideoOverlaySettings GetVideoOverlaySettings();

        NetworkSettings GetDefaultNetworkSettings();

        InputSettings GetDefaultInputSettings();

        Task SaveSettingsAsync(NetworkSettings networkSettings, InputSettings inputSettings, CancellationToken cancellationToken = default);
    }
}