using BatX3_HSS_GUI.Client.ViewModels.Detection;
using BatX3_HSS_GUI.Client.ViewModels.Diagnostics;
using BatX3_HSS_GUI.Client.ViewModels.Operation;
using BatX3_HSS_GUI.Client.ViewModels.Parameters;
using BatX3_HSS_GUI.Client.ViewModels.Synchronization;
using BatX3_HSS_GUI.Client.ViewModels.Video;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace BatX3_HSS_GUI.Client.ViewModels
{
    public sealed class MainWindowViewModel :
            ObservableObject
    {
        public MainWindowViewModel(
            IConfiguration configuration,
            SettingsViewModel settings,
            ParametersViewModel parameters,
            OperationViewModel operation,
            VideoViewModel video,
            DetectionViewModel detection,
            FrameSynchronizationViewModel synchronization,
            DiagnosticsViewModel diagnostics)
        {
            ApplicationTitle =
                configuration["Application:Name"]
                ?? "BAT-X3_HSS_GUI Client";

            VersionDisplayText =
                CreateVersionDisplayText();

            Settings =
                settings;

            Parameters =
                parameters;

            Operation =
                operation;

            Video =
                video;

            Detection =
                detection;

            Synchronization =
                synchronization;

            Diagnostics =
                diagnostics;
        }

        public string ApplicationTitle { get; }

        public string VersionDisplayText { get; }

        public SettingsViewModel Settings { get; }

        public ParametersViewModel Parameters { get; }

        public OperationViewModel Operation { get; }

        public VideoViewModel Video { get; }

        public DetectionViewModel Detection { get; }

        public FrameSynchronizationViewModel Synchronization { get; }

        public DiagnosticsViewModel Diagnostics { get; }

        private static string CreateVersionDisplayText()
        {
            Version? version =
                typeof(MainWindowViewModel)
                    .Assembly
                    .GetName()
                    .Version;

            if (version is null)
            {
                return "v—";
            }

            int build =
                version.Build >= 0
                    ? version.Build
                    : 0;

            return
                $"Beta v{version.Major}.{version.Minor}.{build}";
        }
    }
}