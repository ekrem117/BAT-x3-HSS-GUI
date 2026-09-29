using BatX3_HSS_GUI.Application.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatX3_HSS_GUI.Client.ViewModels.Diagnostics
{
    public partial class ChannelHealthItemViewModel :
           ObservableObject
    {
        public ChannelHealthItemViewModel(
            DiagnosticChannel channel,
            string displayName)
        {
            Channel =
                channel;

            DisplayName =
                displayName;
        }

        public DiagnosticChannel Channel
        {
            get;
        }

        public string DisplayName
        {
            get;
        }

        // ============================================================
        // PRESENTATION
        // ============================================================

        public string StateText =>
            State switch
            {
                "Healthy" =>
                    "AKTİF",

                "Running" =>
                    "AKTİF",

                "Waiting" =>
                    "BEKLİYOR",

                "Starting" =>
                    "BAŞLATILIYOR",

                "Degraded" =>
                    "UYARI",

                "Stale" =>
                    "GÜNCEL DEĞİL",

                "Error" =>
                    "HATA",

                "Faulted" =>
                    "HATA",

                "Failed" =>
                    "HATA",

                "Stopped" =>
                    "DURDU",

                _ =>
                    "BİLİNMİYOR"
            };

        [ObservableProperty]
        private string _endpointText =
            "—";

        [ObservableProperty]
        private string _state =
            "Stopped";

        [ObservableProperty]
        private string _lastActivity =
            "—";

        [ObservableProperty]
        private long _successCount;

        [ObservableProperty]
        private long _errorCount;

        [ObservableProperty]
        private string _lastError =
            "—";

        partial void OnStateChanged(
            string value)
        {
            OnPropertyChanged(
                nameof(StateText));
        }

        // ============================================================
        // SNAPSHOT
        // ============================================================

        public void Apply(
            ChannelHealthSnapshot snapshot)
        {
            State =
                snapshot.State.ToString();

            SuccessCount =
                snapshot.SuccessCount;

            ErrorCount =
                snapshot.ErrorCount;

            LastError =
                string.IsNullOrWhiteSpace(
                    snapshot.LastError)
                    ? "—"
                    : snapshot.LastError;

            LastActivity =
                snapshot.LastSuccessAge is null
                    ? "—"
                    : FormatAge(
                        snapshot.LastSuccessAge.Value);
        }

        private static string FormatAge(
            TimeSpan age)
        {
            if (age.TotalMilliseconds < 1000)
            {
                return
                    $"{age.TotalMilliseconds:0} ms";
            }

            return
                $"{age.TotalSeconds:0.0} s";
        }
    }
}