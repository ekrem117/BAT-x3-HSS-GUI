using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace BatX3_HSS_GUI.Client.ViewModels.Operation
{
    public partial class WeaponControlViewModel :
        ObservableObject
    {
        private readonly IParameterService _parameterService;
        private readonly ISystemRuntimeState _systemRuntimeState;

        private bool _isSynchronizingConfiguration;
        private bool _configurationDirty;

        public WeaponControlViewModel(
            IParameterService parameterService,
            ISystemRuntimeState systemRuntimeState)
        {
            _parameterService =
                parameterService;

            _systemRuntimeState =
                systemRuntimeState;

            FireModeOptions =
            [
                new(
                    0,
                    "Tekli"),

                new(
                    1,
                    "Seri")
            ];

            WeaponSelectionOptions =
            [
                new(
                    0,
                    "Sol"),

                new(
                    1,
                    "Sağ"),

                new(
                    2,
                    "Her İkisi")
            ];

            BurstCountOptions =
                Enumerable.Range(
                        1,
                        10)
                    .ToArray();
        }

        public IReadOnlyList<WeaponOption> FireModeOptions
        {
            get;
        }

        public IReadOnlyList<WeaponOption> WeaponSelectionOptions
        {
            get;
        }

        public IReadOnlyList<int> BurstCountOptions
        {
            get;
        }

        // ============================================================
        // CURRENT SERVER STATE
        // ============================================================

        [ObservableProperty]
        private int? _armedValue;

        public string ArmedText =>
            ArmedValue switch
            {
                0 =>
                    "GÜVENLİ",

                1 =>
                    "YETKİLİ",

                _ =>
                    "Bilinmiyor"
            };

        [ObservableProperty]
        private int? _fireModeValue;

        public string CurrentFireModeText =>
            FireModeValue switch
            {
                0 =>
                    "TEKLİ",

                1 =>
                    "SERİ",

                _ =>
                    "—"
            };

        [ObservableProperty]
        private int? _burstCountValue;

        public string CurrentBurstCountText =>
            BurstCountValue?.ToString(
                CultureInfo.CurrentCulture)
            ?? "—";

        [ObservableProperty]
        private int? _selectedWeaponValue;

        public string CurrentSelectedWeaponText =>
            SelectedWeaponValue switch
            {
                0 =>
                    "SOL",

                1 =>
                    "SAĞ",

                2 =>
                    "HER İKİSİ",

                _ =>
                    "—"
            };

        [ObservableProperty]
        private int? _ammoValue;

        public string AmmoText =>
            AmmoValue?.ToString(
                CultureInfo.CurrentCulture)
            ?? "—";

        [ObservableProperty]
        private int? _shotsFiredValue;

        public string ShotsFiredText =>
            ShotsFiredValue?.ToString(
                CultureInfo.CurrentCulture)
            ?? "—";

        // ============================================================
        // PENDING CONFIGURATION
        // ============================================================

        [ObservableProperty]
        private WeaponOption? _selectedFireModeOption;

        [ObservableProperty]
        private int? _selectedBurstCount;

        [ObservableProperty]
        private WeaponOption? _selectedWeaponOption;

        // ============================================================
        // ARM STATE
        // ============================================================

        [ObservableProperty]
        private bool _isArmedBusy;

        [ObservableProperty]
        private string? _armedErrorMessage;

        public bool HasArmedError =>
            !string.IsNullOrWhiteSpace(
                ArmedErrorMessage);

        // ============================================================
        // CONFIGURATION STATE
        // ============================================================

        [ObservableProperty]
        private bool _isConfigurationBusy;

        [ObservableProperty]
        private string? _configurationErrorMessage;

        [ObservableProperty]
        private string? _configurationStatusMessage;

        public bool HasConfigurationError =>
            !string.IsNullOrWhiteSpace(
                ConfigurationErrorMessage);

        public bool HasConfigurationStatus =>
            !string.IsNullOrWhiteSpace(
                ConfigurationStatusMessage);

        // ============================================================
        // GENERATED PROPERTY NOTIFICATIONS
        // ============================================================

        partial void OnArmedValueChanged(
            int? value)
        {
            OnPropertyChanged(
                nameof(ArmedText));

            NotifyCommandStates();
        }

        partial void OnFireModeValueChanged(
            int? value)
        {
            OnPropertyChanged(
                nameof(CurrentFireModeText));
        }

        partial void OnBurstCountValueChanged(
            int? value)
        {
            OnPropertyChanged(
                nameof(CurrentBurstCountText));
        }

        partial void OnSelectedWeaponValueChanged(
            int? value)
        {
            OnPropertyChanged(
                nameof(CurrentSelectedWeaponText));
        }

        partial void OnAmmoValueChanged(
            int? value)
        {
            OnPropertyChanged(
                nameof(AmmoText));
        }

        partial void OnShotsFiredValueChanged(
            int? value)
        {
            OnPropertyChanged(
                nameof(ShotsFiredText));
        }

        partial void OnSelectedFireModeOptionChanged(
            WeaponOption? value)
        {
            MarkConfigurationDirty();
        }

        partial void OnSelectedBurstCountChanged(
            int? value)
        {
            MarkConfigurationDirty();
        }

        partial void OnSelectedWeaponOptionChanged(
            WeaponOption? value)
        {
            MarkConfigurationDirty();
        }

        partial void OnIsArmedBusyChanged(
            bool value)
        {
            NotifyCommandStates();
        }

        partial void OnIsConfigurationBusyChanged(
            bool value)
        {
            NotifyCommandStates();
        }

        partial void OnArmedErrorMessageChanged(
            string? value)
        {
            OnPropertyChanged(
                nameof(HasArmedError));
        }

        partial void OnConfigurationErrorMessageChanged(
            string? value)
        {
            OnPropertyChanged(
                nameof(HasConfigurationError));
        }

        partial void OnConfigurationStatusMessageChanged(
            string? value)
        {
            OnPropertyChanged(
                nameof(HasConfigurationStatus));
        }

        // ============================================================
        // ARM COMMANDS
        // ============================================================

        [RelayCommand(
            CanExecute = nameof(CanEnableArmed))]
        private async Task EnableArmedAsync()
        {
            await SetArmedAsync(
                1);
        }

        [RelayCommand(
            CanExecute = nameof(CanDisableArmed))]
        private async Task DisableArmedAsync()
        {
            await SetArmedAsync(
                0);
        }

        private bool CanEnableArmed()
        {
            if (IsArmedBusy ||
                IsConfigurationBusy ||
                ArmedValue != 0)
            {
                return false;
            }

            SystemOperatingMode? mode =
                _systemRuntimeState.CurrentMode;

            return
                mode is
                    SystemOperatingMode.Manual or
                    SystemOperatingMode.Auto;
        }

        private bool CanDisableArmed()
        {
            /*
             * Güvenliye alma işlemi mode'dan bağımsızdır.
             *
             * Armed state bilinmese bile SET(0) gönderilebilmesi
             * fail-safe davranıştır.
             */
            return
                !IsArmedBusy &&
                !IsConfigurationBusy;
        }

        private async Task SetArmedAsync(
            int targetValue)
        {
            if (targetValue == 1)
            {
                SystemOperatingMode? mode =
                    _systemRuntimeState.CurrentMode;

                if (mode is not
                        SystemOperatingMode.Manual and not
                        SystemOperatingMode.Auto)
                {
                    ArmedErrorMessage =
                        "Ateş yetkisi yalnızca MANUEL veya OTOMATİK " +
                        "çalışma modunda açılabilir.";

                    return;
                }
            }

            IsArmedBusy =
                true;

            ArmedErrorMessage =
                null;

            try
            {
                ParameterOperationResult result =
                    await _parameterService.SetAsync(
                        ParameterNames.Weapon.Armed,
                        targetValue);

                if (!result.IsSuccess)
                {
                    ArmedErrorMessage =
                        $"Ateş yetkisi değiştirilemedi. " +
                        $"Status: {result.Status}";

                    return;
                }

                /*
                 * Polling yaklaşık 50 ms içinde gerçek server
                 * durumunu tekrar doğrulayacaktır.
                 */
                ArmedValue =
                    targetValue;
            }
            catch (Exception exception)
            {
                /*
                 * SET sonucu belirsiz olabilir.
                 */
                ArmedValue =
                    null;

                ArmedErrorMessage =
                    exception.Message;
            }
            finally
            {
                IsArmedBusy =
                    false;
            }
        }

        // ============================================================
        // CONFIGURATION COMMAND
        // ============================================================

        [RelayCommand(
            CanExecute = nameof(CanApplyConfiguration))]
        private async Task ApplyConfigurationAsync()
        {
            WeaponOption? fireMode =
                SelectedFireModeOption;

            int? burstCount =
                SelectedBurstCount;

            WeaponOption? weaponSelection =
                SelectedWeaponOption;

            if (fireMode is null ||
                burstCount is null ||
                weaponSelection is null)
            {
                return;
            }

            IsConfigurationBusy =
                true;

            ConfigurationErrorMessage =
                null;

            ConfigurationStatusMessage =
                null;

            try
            {
                Dictionary<string, object> parameters =
                    new(StringComparer.Ordinal)
                    {
                        [ParameterNames.Weapon.FireMode] =
                            fireMode.Value,

                        [ParameterNames.Weapon.BurstCount] =
                            burstCount.Value,

                        [ParameterNames.Weapon.Selected] =
                            weaponSelection.Value
                    };

                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await _parameterService.SetAsync(
                            parameters);

                List<string> failures =
                    GetConfigurationFailures(
                        results);

                if (failures.Count > 0)
                {
                    ConfigurationErrorMessage =
                        string.Join(
                            Environment.NewLine,
                            failures);

                    return;
                }

                _configurationDirty =
                    false;

                ConfigurationStatusMessage =
                    "Silah ayarları uygulandı.";

                ApplyConfigurationCommand
                    .NotifyCanExecuteChanged();
            }
            catch (Exception exception)
            {
                ConfigurationErrorMessage =
                    exception.Message;
            }
            finally
            {
                IsConfigurationBusy =
                    false;
            }
        }

        private bool CanApplyConfiguration()
        {
            return
                _configurationDirty &&
                !IsConfigurationBusy &&
                !IsArmedBusy &&
                SelectedFireModeOption is not null &&
                SelectedBurstCount is >= 1 and <= 10 &&
                SelectedWeaponOption is not null;
        }

        private static List<string>
            GetConfigurationFailures(
                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results)
        {
            List<string> failures = new();

            AddFailureIfRequired(
                results,
                ParameterNames.Weapon.FireMode,
                "Atış modu",
                failures);

            AddFailureIfRequired(
                results,
                ParameterNames.Weapon.BurstCount,
                "Seri adedi",
                failures);

            AddFailureIfRequired(
                results,
                ParameterNames.Weapon.Selected,
                "Silah seçimi",
                failures);

            return failures;
        }

        private static void AddFailureIfRequired(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            string parameterName,
            string displayName,
            ICollection<string> failures)
        {
            if (!results.TryGetValue(
                    parameterName,
                    out ParameterOperationResult result))
            {
                failures.Add(
                    $"{displayName}: cevap alınamadı.");

                return;
            }

            if (!result.IsSuccess)
            {
                failures.Add(
                    $"{displayName}: Status {result.Status}.");
            }
        }

        // ============================================================
        // POLLING
        // ============================================================

        public void ApplyPollingResults(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            UpdateArmed(
                results);

            UpdateFireMode(
                results);

            UpdateBurstCount(
                results);

            UpdateSelectedWeapon(
                results);

            UpdateAmmo(
                results);

            UpdateShotsFired(
                results);
        }

        public void MarkUnavailable()
        {
            ArmedValue =
                null;

            FireModeValue =
                null;

            BurstCountValue =
                null;

            SelectedWeaponValue =
                null;

            AmmoValue =
                null;

            ShotsFiredValue =
                null;

            NotifyCommandStates();
        }

        private void UpdateArmed(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            ArmedValue =
                TryGetInt32(
                    results,
                    ParameterNames.Weapon.Armed,
                    0,
                    1,
                    out int value)
                    ? value
                    : null;
        }

        private void UpdateFireMode(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            if (!TryGetInt32(
                    results,
                    ParameterNames.Weapon.FireMode,
                    0,
                    1,
                    out int value))
            {
                FireModeValue =
                    null;

                if (!_configurationDirty &&
                    !IsConfigurationBusy)
                {
                    SynchronizeFireModeSelection(
                        null);
                }

                return;
            }

            FireModeValue =
                value;

            if (!_configurationDirty &&
                !IsConfigurationBusy)
            {
                WeaponOption? option =
                    FireModeOptions.FirstOrDefault(
                        item =>
                            item.Value == value);

                SynchronizeFireModeSelection(
                    option);
            }
        }

        private void UpdateBurstCount(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            if (!TryGetInt32(
                    results,
                    ParameterNames.Weapon.BurstCount,
                    1,
                    10,
                    out int value))
            {
                BurstCountValue =
                    null;

                if (!_configurationDirty &&
                    !IsConfigurationBusy)
                {
                    SynchronizeBurstCount(
                        null);
                }

                return;
            }

            BurstCountValue =
                value;

            if (!_configurationDirty &&
                !IsConfigurationBusy)
            {
                SynchronizeBurstCount(
                    value);
            }
        }

        private void UpdateSelectedWeapon(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            if (!TryGetInt32(
                    results,
                    ParameterNames.Weapon.Selected,
                    0,
                    2,
                    out int value))
            {
                SelectedWeaponValue =
                    null;

                if (!_configurationDirty &&
                    !IsConfigurationBusy)
                {
                    SynchronizeWeaponSelection(
                        null);
                }

                return;
            }

            SelectedWeaponValue =
                value;

            if (!_configurationDirty &&
                !IsConfigurationBusy)
            {
                WeaponOption? option =
                    WeaponSelectionOptions.FirstOrDefault(
                        item =>
                            item.Value == value);

                SynchronizeWeaponSelection(
                    option);
            }
        }

        private void UpdateAmmo(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            AmmoValue =
                TryGetInt32(
                    results,
                    ParameterNames.Weapon.Ammo,
                    0,
                    100,
                    out int value)
                    ? value
                    : null;
        }

        private void UpdateShotsFired(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            ShotsFiredValue =
                TryGetInt32(
                    results,
                    ParameterNames.Weapon.ShotsFired,
                    0,
                    9999,
                    out int value)
                    ? value
                    : null;
        }

        // ============================================================
        // SELECTION SYNCHRONIZATION
        // ============================================================

        private void SynchronizeFireModeSelection(
            WeaponOption? option)
        {
            _isSynchronizingConfiguration =
                true;

            try
            {
                SelectedFireModeOption =
                    option;
            }
            finally
            {
                _isSynchronizingConfiguration =
                    false;
            }
        }

        private void SynchronizeBurstCount(
            int? value)
        {
            _isSynchronizingConfiguration =
                true;

            try
            {
                SelectedBurstCount =
                    value;
            }
            finally
            {
                _isSynchronizingConfiguration =
                    false;
            }
        }

        private void SynchronizeWeaponSelection(
            WeaponOption? option)
        {
            _isSynchronizingConfiguration =
                true;

            try
            {
                SelectedWeaponOption =
                    option;
            }
            finally
            {
                _isSynchronizingConfiguration =
                    false;
            }
        }

        private void MarkConfigurationDirty()
        {
            if (_isSynchronizingConfiguration)
            {
                return;
            }

            _configurationDirty =
                true;

            ConfigurationErrorMessage =
                null;

            ConfigurationStatusMessage =
                null;

            ApplyConfigurationCommand
                .NotifyCanExecuteChanged();
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private void NotifyCommandStates()
        {
            EnableArmedCommand
                .NotifyCanExecuteChanged();

            DisableArmedCommand
                .NotifyCanExecuteChanged();

            ApplyConfigurationCommand
                .NotifyCanExecuteChanged();
        }

        private static bool TryGetInt32(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            string parameterName,
            int minimum,
            int maximum,
            out int value)
        {
            value =
                default;

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

            try
            {
                value =
                    Convert.ToInt32(
                        result.Value,
                        CultureInfo.InvariantCulture);

                return
                    value >= minimum &&
                    value <= maximum;
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
    }

    public sealed record WeaponOption(
        int Value,
        string DisplayName);
}