using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Client.ViewModels.Actions;
using BatX3_HSS_GUI.Client.ViewModels.Video;
using BatX3_HSS_GUI.Domain.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace BatX3_HSS_GUI.Client.ViewModels.Operation
{
    public partial class OperationViewModel :
            ObservableObject
    {
        private readonly ISystemModeService _systemModeService;
        private readonly IParameterService _parameterService;

        private bool _isSynchronizingModeSelection;
        private bool _modeSelectionDirty;

        private bool _isSynchronizingConfidenceThreshold;
        private bool _confidenceThresholdDirty;

        public OperationViewModel(
            VideoViewModel video,
            ISystemModeService systemModeService,
            IParameterService parameterService,
            ISystemRuntimeState systemRuntimeState)
        {
            Video =
                video;

            _systemModeService =
                systemModeService;

            _parameterService =
                parameterService;

            Weapon =
                new WeaponControlViewModel(
                    parameterService,
                    systemRuntimeState);

            ModeOptions =
            [
                new(
                    SystemOperatingMode.Idle,
                    "Bekleme"),

                new(
                    SystemOperatingMode.Manual,
                    "Manuel"),

                new(
                    SystemOperatingMode.Auto,
                    "Otomatik"),

                new(
                    SystemOperatingMode.LoopDemo,
                    "Döngü Demo"),

                new(
                    SystemOperatingMode.EmergencyStop,
                    "Acil Durdurma")
            ];
        }

        public VideoViewModel Video { get; }

        public WeaponControlViewModel Weapon { get; }

        public IReadOnlyList<SystemModeOption> ModeOptions
        {
            get;
        }

        // ============================================================
        // SYSTEM MODE
        // ============================================================

        [ObservableProperty]
        private SystemModeOption? _selectedModeOption;

        [ObservableProperty]
        private bool _isModeChangeBusy;

        [ObservableProperty]
        private string? _modeChangeErrorMessage;

        public bool HasModeChangeError =>
            !string.IsNullOrWhiteSpace(
                ModeChangeErrorMessage);

        // ============================================================
        // LIVE STATUS
        // ============================================================

        [ObservableProperty]
        private string _modeText =
            "Bilinmiyor";

        [ObservableProperty]
        private string _linkText =
            "Bilinmiyor";

        [ObservableProperty]
        private string _fpsText =
            "—";

        [ObservableProperty]
        private string _detectionCountText =
            "—";

        [ObservableProperty]
        private string _inferenceText =
            "—";

        // ============================================================
        // QUICK VISION SETTING
        // ============================================================

        [ObservableProperty]
        private string _confidenceThresholdText =
            string.Empty;

        [ObservableProperty]
        private bool _isConfidenceThresholdBusy;

        [ObservableProperty]
        private string? _confidenceThresholdErrorMessage;

        public bool HasConfidenceThresholdError =>
            !string.IsNullOrWhiteSpace(
                ConfidenceThresholdErrorMessage);

        // ============================================================
        // PROPERTY CHANGE HANDLERS
        // ============================================================

        partial void OnSelectedModeOptionChanged(
            SystemModeOption? value)
        {
            if (!_isSynchronizingModeSelection)
            {
                _modeSelectionDirty =
                    true;
            }

            ApplyModeCommand
                .NotifyCanExecuteChanged();
        }

        partial void OnIsModeChangeBusyChanged(
            bool value)
        {
            ApplyModeCommand
                .NotifyCanExecuteChanged();
        }

        partial void OnModeChangeErrorMessageChanged(
            string? value)
        {
            OnPropertyChanged(
                nameof(HasModeChangeError));
        }

        partial void OnConfidenceThresholdTextChanged(
            string value)
        {
            if (!_isSynchronizingConfidenceThreshold)
            {
                _confidenceThresholdDirty =
                    true;

                ConfidenceThresholdErrorMessage =
                    null;
            }

            ApplyConfidenceThresholdCommand
                .NotifyCanExecuteChanged();
        }

        partial void OnIsConfidenceThresholdBusyChanged(
            bool value)
        {
            ApplyConfidenceThresholdCommand
                .NotifyCanExecuteChanged();
        }

        partial void OnConfidenceThresholdErrorMessageChanged(
            string? value)
        {
            OnPropertyChanged(
                nameof(HasConfidenceThresholdError));
        }

        // ============================================================
        // SYSTEM MODE COMMAND
        // ============================================================

        [RelayCommand(
            CanExecute = nameof(CanApplyMode))]
        private async Task ApplyModeAsync()
        {
            SystemModeOption? selectedMode =
                SelectedModeOption;

            if (selectedMode is null)
            {
                return;
            }

            IsModeChangeBusy =
                true;

            ModeChangeErrorMessage =
                null;

            try
            {
                await _systemModeService.SetModeAsync(
                    selectedMode.Mode);

                _modeSelectionDirty =
                    false;
            }
            catch (Exception exception)
            {
                ModeChangeErrorMessage =
                    exception.Message;
            }
            finally
            {
                IsModeChangeBusy =
                    false;
            }
        }

        private bool CanApplyMode()
        {
            return
                !IsModeChangeBusy &&
                SelectedModeOption is not null;
        }

        // ============================================================
        // CONFIDENCE THRESHOLD COMMAND
        // ============================================================

        [RelayCommand(
            CanExecute = nameof(CanApplyConfidenceThreshold))]
        private async Task ApplyConfidenceThresholdAsync()
        {
            if (!TryParseConfidenceThreshold(
                    out double value))
            {
                ConfidenceThresholdErrorMessage =
                    "Tespit güven eşiği 0 ile 1 arasında geçerli bir sayı olmalıdır.";

                return;
            }

            IsConfidenceThresholdBusy =
                true;

            ConfidenceThresholdErrorMessage =
                null;

            try
            {
                ParameterOperationResult result =
                    await _parameterService.SetAsync(
                        ParameterNames.Vision.ConfidenceThreshold,
                        value);

                if (!result.IsSuccess)
                {
                    ConfidenceThresholdErrorMessage =
                        $"Tespit güven eşiği değiştirilemedi. " +
                        $"Status: {result.Status}";

                    return;
                }

                _confidenceThresholdDirty =
                    false;

                SynchronizeConfidenceThresholdText(
                    value);
            }
            catch (Exception exception)
            {
                ConfidenceThresholdErrorMessage =
                    exception.Message;
            }
            finally
            {
                IsConfidenceThresholdBusy =
                    false;
            }
        }

        private bool CanApplyConfidenceThreshold()
        {
            return
                _confidenceThresholdDirty &&
                !IsConfidenceThresholdBusy;
        }

        private bool TryParseConfidenceThreshold(
            out double value)
        {
            value =
                default;

            if (!double.TryParse(
                    ConfidenceThresholdText,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out value))
            {
                return false;
            }

            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                value >= 0.0 &&
                value <= 1.0;
        }

        // ============================================================
        // POLLING
        // ============================================================

        public void ApplyPollingResults(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            UpdateMode(
                results);

            UpdateLink(
                results);

            UpdateConfidenceThreshold(
                results);

            Weapon.ApplyPollingResults(
                results);

            FpsText =
                TryGetDouble(
                    results,
                    ParameterNames.System.Fps,
                    out double fps)
                    ? $"{fps.ToString(
                        "0.0",
                        CultureInfo.CurrentCulture)} fps"
                    : "—";

            DetectionCountText =
                TryGetInt32(
                    results,
                    ParameterNames.Vision.DetectionCount,
                    out int detectionCount)
                    ? detectionCount.ToString(
                        CultureInfo.CurrentCulture)
                    : "—";

            InferenceText =
                TryGetDouble(
                    results,
                    ParameterNames.Vision.InferenceMilliseconds,
                    out double inferenceMilliseconds)
                    ? $"{inferenceMilliseconds.ToString(
                        "0.0",
                        CultureInfo.CurrentCulture)} ms"
                    : "—";
        }

        public void MarkUnavailable()
        {
            ModeText =
                "Bilinmiyor";

            LinkText =
                "Bilinmiyor";

            FpsText =
                "—";

            DetectionCountText =
                "—";

            InferenceText =
                "—";

            if (!_confidenceThresholdDirty &&
                !IsConfidenceThresholdBusy)
            {
                SynchronizeConfidenceThresholdText(
                    null);
            }

            Weapon.MarkUnavailable();
        }

        private void UpdateMode(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            if (!TryGetInt32(
                    results,
                    ParameterNames.System.Mode,
                    out int modeValue) ||
                !Enum.IsDefined(
                    typeof(SystemOperatingMode),
                    modeValue))
            {
                ModeText =
                    "Bilinmiyor";

                return;
            }

            SystemOperatingMode mode =
                (SystemOperatingMode)modeValue;

            ModeText =
                GetModeDisplayName(
                    mode)
                .ToUpper(
                    CultureInfo.CurrentCulture);

            if (!_modeSelectionDirty &&
                !IsModeChangeBusy)
            {
                SynchronizeModeSelection(
                    mode);
            }
        }

        private void SynchronizeModeSelection(
            SystemOperatingMode mode)
        {
            SystemModeOption? option =
                ModeOptions.FirstOrDefault(
                    item =>
                        item.Mode == mode);

            if (option is null)
            {
                return;
            }

            _isSynchronizingModeSelection =
                true;

            try
            {
                SelectedModeOption =
                    option;
            }
            finally
            {
                _isSynchronizingModeSelection =
                    false;
            }
        }

        private void UpdateLink(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            if (!TryGetInt32(
                    results,
                    ParameterNames.System.Link,
                    out int linkValue) ||
                !Enum.IsDefined(
                    typeof(SystemLinkState),
                    linkValue))
            {
                LinkText =
                    "Bilinmiyor";

                return;
            }

            SystemLinkState linkState =
                (SystemLinkState)linkValue;

            LinkText =
                linkState switch
                {
                    SystemLinkState.Disconnected =>
                        "BAĞLANTI YOK",

                    SystemLinkState.Connected =>
                        "BAĞLI",

                    SystemLinkState.Timeout =>
                        "ZAMAN AŞIMI",

                    _ =>
                        "Bilinmiyor"
                };
        }

        private void UpdateConfidenceThreshold(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            if (_confidenceThresholdDirty ||
                IsConfidenceThresholdBusy)
            {
                return;
            }

            if (!TryGetDouble(
                    results,
                    ParameterNames.Vision.ConfidenceThreshold,
                    out double value) ||
                value < 0.0 ||
                value > 1.0)
            {
                SynchronizeConfidenceThresholdText(
                    null);

                return;
            }

            SynchronizeConfidenceThresholdText(
                value);
        }

        private void SynchronizeConfidenceThresholdText(
            double? value)
        {
            _isSynchronizingConfidenceThreshold =
                true;

            try
            {
                ConfidenceThresholdText =
                    value?.ToString(
                        "0.00",
                        CultureInfo.CurrentCulture)
                    ?? string.Empty;
            }
            finally
            {
                _isSynchronizingConfidenceThreshold =
                    false;
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static string GetModeDisplayName(
            SystemOperatingMode mode)
        {
            return mode switch
            {
                SystemOperatingMode.Idle =>
                    "Bekleme",

                SystemOperatingMode.Manual =>
                    "Manuel",

                SystemOperatingMode.Auto =>
                    "Otomatik",

                SystemOperatingMode.LoopDemo =>
                    "Döngü Demo",

                SystemOperatingMode.EmergencyStop =>
                    "Acil Durdurma",

                _ =>
                    "Bilinmiyor"
            };
        }

        private static bool TryGetInt32(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            string parameterName,
            out int value)
        {
            value =
                default;

            if (!TryGetSuccessfulValue(
                    results,
                    parameterName,
                    out object? rawValue))
            {
                return false;
            }

            try
            {
                value =
                    Convert.ToInt32(
                        rawValue,
                        CultureInfo.InvariantCulture);

                return true;
            }
            catch (Exception exception)
                when (exception is
                    FormatException or
                    InvalidCastException or
                    OverflowException)
            {
                return false;
            }
        }

        private static bool TryGetDouble(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            string parameterName,
            out double value)
        {
            value =
                default;

            if (!TryGetSuccessfulValue(
                    results,
                    parameterName,
                    out object? rawValue))
            {
                return false;
            }

            try
            {
                value =
                    Convert.ToDouble(
                        rawValue,
                        CultureInfo.InvariantCulture);

                if (double.IsNaN(value) ||
                    double.IsInfinity(value))
                {
                    return false;
                }

                return true;
            }
            catch (Exception exception)
                when (exception is
                    FormatException or
                    InvalidCastException or
                    OverflowException)
            {
                return false;
            }
        }

        private static bool TryGetSuccessfulValue(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            string parameterName,
            out object? value)
        {
            value =
                null;

            if (!results.TryGetValue(
                    parameterName,
                    out ParameterOperationResult result))
            {
                return false;
            }

            if (!result.IsSuccess ||
                result.Value is null)
            {
                return false;
            }

            value =
                result.Value;

            return true;
        }
    }

    public sealed record SystemModeOption(
        SystemOperatingMode Mode,
        string DisplayName);
}