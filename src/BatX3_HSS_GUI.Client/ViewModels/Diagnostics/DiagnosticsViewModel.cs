using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BatX3_HSS_GUI.Client.ViewModels.Diagnostics
{
    public partial class DiagnosticsViewModel :
        ObservableObject
    {
        private readonly ISettingsService?
            _settingsService;

        /*
         * Parameterless constructor testlerin ve design-time
         * kullanımın mevcut davranışını korur.
         */
        public DiagnosticsViewModel()
        {
            Command =
                new ChannelHealthItemViewModel(
                    DiagnosticChannel.Command,
                    "Komut");

            Video =
                new ChannelHealthItemViewModel(
                    DiagnosticChannel.Video,
                    "Video");

            Detection =
                new ChannelHealthItemViewModel(
                    DiagnosticChannel.Detection,
                    "Tespit");

            Channels =
            [
                Command,
                Video,
                Detection
            ];
        }

        /*
         * Runtime DI bu constructor'ı kullanır.
         */
        public DiagnosticsViewModel(
            ISettingsService settingsService)
            : this()
        {
            _settingsService =
                settingsService;

            RefreshConfiguration();
        }

        public ChannelHealthItemViewModel Command
        {
            get;
        }

        public ChannelHealthItemViewModel Video
        {
            get;
        }

        public ChannelHealthItemViewModel Detection
        {
            get;
        }

        public ObservableCollection<
            ChannelHealthItemViewModel> Channels
        {
            get;
        }

        // ============================================================
        // SYNCHRONIZATION METRICS
        // ============================================================

        [ObservableProperty]
        private long _matchedFrames;

        [ObservableProperty]
        private long _videoOnlyFrames;

        [ObservableProperty]
        private long _droppedDetectionFrames;

        // ============================================================
        // ACTIVE CONFIGURATION
        // ============================================================

        public void RefreshConfiguration()
        {
            if (_settingsService is null)
            {
                return;
            }

            NetworkSettings settings =
                _settingsService.GetNetworkSettings();

            Command.EndpointText =
                $"{settings.ServerIp}:{settings.Command.RemotePort}";

            Video.EndpointText =
                $"{settings.LocalBindIp}:{settings.Video.ListenPort}";

            Detection.EndpointText =
                $"{settings.LocalBindIp}:{settings.Detection.ListenPort}";
        }

        // ============================================================
        // CHANNEL SNAPSHOT
        // ============================================================

        public void ApplyChannelSnapshot(
            ChannelHealthSnapshot snapshot)
        {
            ChannelHealthItemViewModel? item =
                snapshot.Channel switch
                {
                    DiagnosticChannel.Command =>
                        Command,

                    DiagnosticChannel.Video =>
                        Video,

                    DiagnosticChannel.Detection =>
                        Detection,

                    _ =>
                        null
                };

            item?.Apply(
                snapshot);
        }
    }
}