using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Configuration.Runtime;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Client.Input;
using BatX3_HSS_GUI.Client.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BatX3_HSS_GUI.Client.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService
            _settingsService;

        private readonly IApplicationActionCatalog
            _applicationActionCatalog;

        private readonly InputSettingsValidator
            _inputSettingsValidator;

        private readonly IKeyboardShortcutService
            _keyboardShortcutService;

        private readonly IRuntimeSettingsApplyService
            _runtimeSettingsApplyService;

        private readonly IGamepadDeviceProvider
            _gamepadDeviceProvider;

        private readonly GamepadControlCaptureService
            _gamepadControlCaptureService;

        private readonly IReadOnlyList<string>
            _availableKeys;

        private readonly IReadOnlyList<
            GamepadControlOptionViewModel>
            _availableGamepadControls;

        private GamepadBindingEditorItemViewModel?
            _activeGamepadCaptureEditor;

        private long
            _gamepadCaptureGeneration;

        public SettingsViewModel(
            ISettingsService settingsService,
            IApplicationActionCatalog applicationActionCatalog,
            InputSettingsValidator inputSettingsValidator,
            IKeyboardShortcutService keyboardShortcutService,
            IRuntimeSettingsApplyService runtimeSettingsApplyService,
            IGamepadDeviceProvider gamepadDeviceProvider,
            GamepadControlCaptureService gamepadControlCaptureService)
        {
            _settingsService =
                settingsService;

            _applicationActionCatalog =
                applicationActionCatalog;

            _inputSettingsValidator =
                inputSettingsValidator;

            _keyboardShortcutService =
                keyboardShortcutService;

            _runtimeSettingsApplyService =
                runtimeSettingsApplyService;

            _gamepadDeviceProvider =
                gamepadDeviceProvider;

            _gamepadControlCaptureService =
                gamepadControlCaptureService;

            _availableKeys =
                KeyboardKeyCatalog.GetAvailableKeys();

            _availableGamepadControls =
                GamepadControlCatalog
                    .GetAvailableControls();

            LoadSettings();
        }

        // ================================================================
        // NETWORK SETTINGS
        // ================================================================

        [ObservableProperty]
        private string _serverIp =
            "127.0.0.1";

        [ObservableProperty]
        private string _localBindIp =
            "127.0.0.1";

        [ObservableProperty]
        private int _commandRemotePort =
            2023;

        [ObservableProperty]
        private int _commandLocalPort;

        [ObservableProperty]
        private int _commandResponseTimeoutMs =
            300;

        [ObservableProperty]
        private int _videoListenPort =
            2024;

        [ObservableProperty]
        private int _videoReceiveBufferBytes =
            4 * 1024 * 1024;

        [ObservableProperty]
        private int _detectionListenPort =
            2025;

        [ObservableProperty]
        private int _detectionStaleTimeoutMs =
            500;

        // ================================================================
        // GAMEPAD SETTINGS
        // ================================================================

        [ObservableProperty]
        private bool _gamepadEnabled =
            true;

        [ObservableProperty]
        private string? _selectedGamepadDeviceId;

        [ObservableProperty]
        private bool _gamepadInvertY =
            true;

        [ObservableProperty]
        private double _gamepadDeadzone =
            0.15;

        [ObservableProperty]
        private double _gamepadTriggerThreshold =
            0.50;

        [ObservableProperty]
        private int _gamepadTriggerDebounceSamples =
            3;

        [ObservableProperty]
        private int _gamepadPollingIntervalMs =
            10;

        [ObservableProperty]
        private int _gamepadFireMinimumIntervalMs =
            1000;

        [ObservableProperty]
        private string _gamepadRuntimeStatusText =
            "BAĞLANTI YOK";

        [ObservableProperty]
        private string _gamepadRuntimeDeviceText =
            "—";

        [ObservableProperty]
        private string _gamepadRuntimeDetailText =
            "Gamepad bağlı değil.";

        [ObservableProperty]
        private bool _gamepadRuntimeHasError;

        [ObservableProperty]
        private bool _gamepadRuntimeIsReady;

        public ObservableCollection<
            GamepadDeviceOptionViewModel>
            GamepadDevices
        {
            get;
        } =
            new();

        public ObservableCollection<
            GamepadBindingEditorItemViewModel>
            GamepadBindings
        {
            get;
        } =
            new();

        [RelayCommand]
        private void RefreshGamepadDevices()
        {
            CancelGamepadCapture();

            RefreshGamepadDeviceList(
                SelectedGamepadDeviceId);

            HasError =
                false;

            StatusMessage =
                "Bağlı gamepad listesi yenilendi.";
        }

        [RelayCommand(
            AllowConcurrentExecutions = true)]
        private async Task ToggleGamepadControlCaptureAsync(
            GamepadBindingEditorItemViewModel? binding)
        {
            if (binding is null ||
                IsBusy ||
                !GamepadEnabled)
            {
                return;
            }

            if (binding.IsCapturing)
            {
                CancelGamepadCapture();

                HasError =
                    false;

                StatusMessage =
                    "Gamepad kontrol ataması iptal edildi.";

                return;
            }

            /*
             * Keyboard ve gamepad capture aynı anda aktif olamaz.
             */
            CancelShortcutCapture();

            /*
             * Başka bir binding satırında capture devam ediyorsa
             * yeni capture başlamadan önce kapatılır.
             */
            CancelGamepadCapture();

            try
            {
                IReadOnlyList<GamepadDeviceInfo> devices =
                    _gamepadDeviceProvider
                        .GetConnectedDevices();

                if (devices.Count == 0)
                {
                    HasError =
                        true;

                    StatusMessage =
                        "Kontrol ataması için bağlı bir gamepad bulunamadı.";

                    return;
                }

                if (!string.IsNullOrWhiteSpace(
                        SelectedGamepadDeviceId) &&
                    !devices.Any(
                        device =>
                            string.Equals(
                                device.Id,
                                SelectedGamepadDeviceId,
                                StringComparison.Ordinal)))
                {
                    HasError =
                        true;

                    StatusMessage =
                        "Seçili gamepad şu anda bağlı değil.";

                    return;
                }

                long captureGeneration =
                    checked(
                        ++_gamepadCaptureGeneration);

                _activeGamepadCaptureEditor =
                    binding;

                binding.IsCapturing =
                    true;

                HasError =
                    false;

                StatusMessage =
                    $"'{binding.DisplayName}' için gamepad üzerinde " +
                    "atamak istediğiniz kontrole basın.";

                GamepadControl? capturedControl =
                    await _gamepadControlCaptureService
                        .CaptureAsync(
                            SelectedGamepadDeviceId);

                /*
                 * Await devam ederken capture iptal edilmiş veya başka
                 * bir capture başlatılmış olabilir.
                 */
                if (captureGeneration !=
                        _gamepadCaptureGeneration ||
                    !ReferenceEquals(
                        _activeGamepadCaptureEditor,
                        binding))
                {
                    return;
                }

                if (capturedControl is null)
                {
                    HasError =
                        false;

                    StatusMessage =
                        "Gamepad kontrol ataması iptal edildi.";

                    return;
                }

                string resultMessage =
                    ApplyCapturedGamepadControl(
                        binding,
                        capturedControl.Value);

                HasError =
                    false;

                StatusMessage =
                    resultMessage;
            }
            catch (OperationCanceledException)
            {
                HasError =
                    false;

                StatusMessage =
                    "Gamepad kontrol ataması iptal edildi.";
            }
            catch (Exception exception)
            {
                HasError =
                    true;

                StatusMessage =
                    $"Gamepad kontrol ataması başarısız: " +
                    $"{exception.Message}";
            }
            finally
            {
                if (ReferenceEquals(
                        _activeGamepadCaptureEditor,
                        binding))
                {
                    binding.IsCapturing =
                        false;

                    _activeGamepadCaptureEditor =
                        null;
                }
            }
        }

        private string ApplyCapturedGamepadControl(
            GamepadBindingEditorItemViewModel target,
            GamepadControl capturedControl)
        {
            GamepadControl previousControl =
                target.SelectedControl;

            string capturedDisplayName =
                GetGamepadControlDisplayName(
                    capturedControl);

            if (previousControl ==
                capturedControl)
            {
                return
                    $"'{target.DisplayName}' zaten " +
                    $"'{capturedDisplayName}' kontrolüne atanmış.";
            }

            GamepadBindingEditorItemViewModel?
                occupiedBinding =
                    GamepadBindings
                        .FirstOrDefault(
                            candidate =>
                                !ReferenceEquals(
                                    candidate,
                                    target) &&
                                candidate.SelectedControl ==
                                    capturedControl);

            if (occupiedBinding is not null)
            {
                /*
                 * Duplicate binding üretmek yerine iki action'ın
                 * fiziksel kontrolleri kontrollü olarak yer değiştirir.
                 */
                occupiedBinding.SelectedControl =
                    previousControl;

                target.SelectedControl =
                    capturedControl;

                return
                    $"'{target.DisplayName}' için " +
                    $"'{capturedDisplayName}' seçildi. " +
                    $"Çakışan '{occupiedBinding.DisplayName}' ataması " +
                    "önceki kontrole taşındı. " +
                    "Kaydet ve Uygula ile etkinleştirin.";
            }

            target.SelectedControl =
                capturedControl;

            return
                $"'{target.DisplayName}' için " +
                $"'{capturedDisplayName}' seçildi. " +
                "Kaydet ve Uygula ile etkinleştirin.";
        }

        private string GetGamepadControlDisplayName(
            GamepadControl control)
        {
            GamepadControlOptionViewModel? option =
                _availableGamepadControls
                    .FirstOrDefault(
                        candidate =>
                            candidate.Control ==
                            control);

            return option?.DisplayName ??
                control.ToString();
        }

        partial void OnGamepadEnabledChanged(
            bool value)
        {
            if (value)
            {
                return;
            }

            if (_activeGamepadCaptureEditor is null &&
                !_gamepadControlCaptureService
                    .GetSnapshot()
                    .IsActive)
            {
                return;
            }

            CancelGamepadCapture();

            HasError =
                false;

            StatusMessage =
                "Gamepad devre dışı bırakıldığı için " +
                "kontrol ataması iptal edildi.";
        }


        // ================================================================
        // UI STATE
        // ================================================================

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyCanExecuteChangedFor(nameof(ReloadCommand))]
        [NotifyCanExecuteChangedFor(nameof(RestoreDefaultsCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _statusMessage =
            string.Empty;

        // ================================================================
        // SHORTCUT SETTINGS
        // ================================================================

        public ObservableCollection<
            ShortcutEditorItemViewModel>
            Shortcuts
        {
            get;
        } =
            new();

        [RelayCommand]
        private async Task ToggleShortcutCaptureAsync(
            ShortcutEditorItemViewModel? shortcut)
        {
            if (shortcut is null ||
                IsBusy)
            {
                return;
            }

            if (shortcut.IsCapturing)
            {
                CancelShortcutCapture();

                HasError =
                    false;

                StatusMessage =
                    "Kısayol ataması iptal edildi.";

                return;
            }

            /*
             * Gamepad capture ile keyboard capture aynı anda
             * aktif tutulmaz.
             */
            CancelGamepadCapture();

            try
            {
                /*
                 * Capture başlamadan önce olası aktif momentary
                 * action'lar fail-safe olarak release edilir.
                 */
                await _keyboardShortcutService
                    .ReleaseAllAsync();
            }
            catch (Exception exception)
            {
                HasError =
                    true;

                StatusMessage =
                    $"Kısayol atama modu başlatılamadı: " +
                    $"{exception.Message}";

                return;
            }

            CancelShortcutCapture();

            shortcut.IsCapturing =
                true;

            HasError =
                false;

            StatusMessage =
                $"'{shortcut.ShortDisplayName}' için yeni " +
                "kısayola basın. Esc ile iptal edebilirsiniz.";

            _keyboardShortcutService.BeginCapture(
                (key, modifiers) =>
                {
                    shortcut.ApplyCapturedGesture(
                        key,
                        modifiers);

                    shortcut.IsCapturing =
                        false;

                    HasError =
                        false;

                    StatusMessage =
                        $"'{shortcut.ShortDisplayName}' için " +
                        $"'{shortcut.ShortcutText}' seçildi. " +
                        "Kaydet ve Uygula ile etkinleştirin.";
                },
                () =>
                {
                    shortcut.IsCapturing =
                        false;
                });
        }

        // ================================================================
        // SAVE
        // ================================================================

        [RelayCommand(
            CanExecute = nameof(CanExecuteSettingsCommand))]
        private async Task SaveAsync()
        {
            if (IsBusy)
            {
                return;
            }

            CancelAllInputCaptures();

            IsBusy =
                true;

            HasError =
                false;

            StatusMessage =
                "Ayarlar uygulanıyor...";

            try
            {
                NetworkSettings networkSettings =
                    CreateNetworkSettings();

                IReadOnlyList<string>
                    networkValidationErrors =
                        NetworkSettingsValidator.Validate(
                            networkSettings);

                if (networkValidationErrors.Count > 0)
                {
                    HasError =
                        true;

                    StatusMessage =
                        string.Join(
                            Environment.NewLine,
                            networkValidationErrors);

                    return;
                }

                InputSettings inputSettings =
                    CreateInputSettings();

                IReadOnlyList<string>
                    inputValidationErrors =
                        _inputSettingsValidator.Validate(
                            inputSettings);

                if (inputValidationErrors.Count > 0)
                {
                    HasError =
                        true;

                    StatusMessage =
                        string.Join(
                            Environment.NewLine,
                            inputValidationErrors);

                    return;
                }

                RuntimeSettingsApplyResult applyResult =
                    await _runtimeSettingsApplyService
                        .ApplyAsync(
                            networkSettings,
                            inputSettings);

                _keyboardShortcutService.Reload();

                LoadSettings();

                HasError =
                    false;

                if (applyResult.NetworkReconfigured)
                {
                    StatusMessage =
                        "Ayarlar kaydedildi ve ağ bağlantıları " +
                        "yeniden yapılandırıldı.";
                }
                else
                {
                    StatusMessage =
                        "Ayarlar kaydedildi.";
                }
            }
            catch (
                RuntimeSettingsApplyException exception)
            {
                HasError =
                    true;

                LoadSettings();

                _keyboardShortcutService.Reload();

                if (exception.RollbackSucceeded)
                {
                    StatusMessage =
                        "Ayarlar uygulanamadı. Önceki çalışan " +
                        "ayarlara geri dönüldü.";
                }
                else
                {
                    StatusMessage =
                        "Ayarlar uygulanamadı ve geri alma işlemi " +
                        "tamamlanamadı. Sistem Durumu ekranını kontrol edin.";
                }
            }
            catch (OperationCanceledException)
            {
                HasError =
                    true;

                StatusMessage =
                    "Ayarların uygulanması iptal edildi.";
            }
            catch (Exception exception)
            {
                HasError =
                    true;

                StatusMessage =
                    $"Ayarlar uygulanamadı: {exception.Message}";
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        // ================================================================
        // RELOAD
        // ================================================================

        [RelayCommand(
            CanExecute = nameof(CanExecuteSettingsCommand))]
        private void Reload()
        {
            if (IsBusy)
            {
                return;
            }

            CancelAllInputCaptures();

            HasError =
                false;

            try
            {
                LoadSettings();

                StatusMessage =
                    "Kaydedilmiş ayarlar yeniden yüklendi.";
            }
            catch (Exception exception)
            {
                HasError =
                    true;

                StatusMessage =
                    $"Ayarlar yeniden yüklenemedi: " +
                    $"{exception.Message}";
            }
        }

        // ================================================================
        // RESTORE DEFAULTS
        // ================================================================

        [RelayCommand(
            CanExecute = nameof(CanExecuteSettingsCommand))]
        private void RestoreDefaults()
        {
            if (IsBusy)
            {
                return;
            }

            CancelAllInputCaptures();

            try
            {
                NetworkSettings defaultNetworkSettings =
                    _settingsService
                        .GetDefaultNetworkSettings();

                InputSettings defaultInputSettings =
                    _settingsService
                        .GetDefaultInputSettings();

                ApplyNetworkSettings(
                    defaultNetworkSettings);

                ApplyInputSettings(
                    defaultInputSettings);

                HasError =
                    false;

                StatusMessage =
                    "Varsayılan ayarlar forma yüklendi. " +
                    "Etkinleştirmek için Kaydet ve Uygula'ya basın.";
            }
            catch (Exception exception)
            {
                HasError =
                    true;

                StatusMessage =
                    $"Varsayılan ayarlar yüklenemedi: " +
                    $"{exception.Message}";
            }
        }

        private bool CanExecuteSettingsCommand()
        {
            return !IsBusy;
        }

        // ================================================================
        // LOAD SETTINGS
        // ================================================================

        private void LoadSettings()
        {
            ApplyNetworkSettings(
                _settingsService.GetNetworkSettings());

            ApplyInputSettings(
                _settingsService.GetInputSettings());
        }

        private void ApplyNetworkSettings(
            NetworkSettings settings)
        {
            ServerIp =
                settings.ServerIp;

            LocalBindIp =
                settings.LocalBindIp;

            CommandRemotePort =
                settings.Command.RemotePort;

            CommandLocalPort =
                settings.Command.LocalPort;

            CommandResponseTimeoutMs =
                settings.Command.ResponseTimeoutMs;

            VideoListenPort =
                settings.Video.ListenPort;

            VideoReceiveBufferBytes =
                settings.Video.ReceiveBufferBytes;

            DetectionListenPort =
                settings.Detection.ListenPort;

            DetectionStaleTimeoutMs =
                settings.Detection.StaleTimeoutMs;
        }

        private void ApplyInputSettings(
            InputSettings inputSettings)
        {
            ArgumentNullException.ThrowIfNull(
                inputSettings);

            ArgumentNullException.ThrowIfNull(
                inputSettings.Gamepad);

            ApplyGamepadSettings(
                inputSettings.Gamepad);

            Shortcuts.Clear();

            IEnumerable<ApplicationActionDefinition> actions =
                _applicationActionCatalog
                    .GetAll()
                    .Where(
                        action =>
                            action.AllowKeyboardShortcut);

            foreach (
                ApplicationActionDefinition action
                in actions)
            {
                ShortcutBindingSettings? binding =
                    inputSettings.Shortcuts
                        .FirstOrDefault(
                            shortcut =>
                                string.Equals(
                                    shortcut.ActionId,
                                    action.Id,
                                    StringComparison.Ordinal));

                ShortcutEditorItemViewModel editor =
                    new(
                        action,
                        binding,
                        _availableKeys);

                Shortcuts.Add(
                    editor);
            }
        }

        private void ApplyGamepadSettings(
            GamepadSettings settings)
        {
            GamepadEnabled =
                settings.Enabled;

            SelectedGamepadDeviceId =
                settings.ActiveDeviceId;

            GamepadInvertY =
                settings.InvertY;

            GamepadDeadzone =
                settings.Deadzone;

            GamepadTriggerThreshold =
                settings.TriggerThreshold;

            GamepadTriggerDebounceSamples =
                settings.TriggerDebounceSamples;

            GamepadPollingIntervalMs =
                settings.PollingIntervalMs;

            GamepadFireMinimumIntervalMs =
                settings.FireMinimumIntervalMs;

            RefreshGamepadDeviceList(
                settings.ActiveDeviceId);

            LoadGamepadBindings(
                settings);
        }

        public void ApplyGamepadRuntimeSnapshot(
            GamepadRuntimeSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(
                snapshot);

            GamepadRuntimeHasError =
                snapshot.Status ==
                GamepadRuntimeStatus.Error;

            GamepadRuntimeIsReady =
                snapshot.Status ==
                GamepadRuntimeStatus.Ready;

            GamepadRuntimeDeviceText =
                !string.IsNullOrWhiteSpace(
                    snapshot.DeviceName)
                    ? snapshot.DeviceName
                    : !string.IsNullOrWhiteSpace(
                        snapshot.DeviceId)
                        ? snapshot.DeviceId
                        : "—";

            switch (snapshot.Status)
            {
                case GamepadRuntimeStatus.Disabled:
                    {
                        GamepadRuntimeStatusText =
                            "DEVRE DIŞI";

                        GamepadRuntimeDetailText =
                            "Gamepad girişi ayarlardan devre dışı.";

                        break;
                    }

                case GamepadRuntimeStatus.Disconnected:
                    {
                        GamepadRuntimeStatusText =
                            "BAĞLANTI YOK";

                        GamepadRuntimeDetailText =
                            "Seçili gamepad bağlı değil.";

                        break;
                    }

                case GamepadRuntimeStatus.NeutralRequired:
                    {
                        GamepadRuntimeStatusText =
                            "NÖTR BEKLENİYOR";

                        GamepadRuntimeDetailText =
                            "Gamepad kontrollerini nötr konuma getirin.";

                        break;
                    }

                case GamepadRuntimeStatus.Ready:
                    {
                        GamepadRuntimeStatusText =
                            "HAZIR";

                        GamepadRuntimeDetailText =
                            "Gamepad kontrolü aktif.";

                        break;
                    }

                case GamepadRuntimeStatus.Error:
                    {
                        GamepadRuntimeStatusText =
                            "HATA";

                        GamepadRuntimeDetailText =
                            string.IsNullOrWhiteSpace(
                                snapshot.LastError)
                                ? "Gamepad runtime hatası oluştu."
                                : snapshot.LastError;

                        break;
                    }

                default:
                    {
                        GamepadRuntimeStatusText =
                            "BİLİNMİYOR";

                        GamepadRuntimeDetailText =
                            "Gamepad runtime durumu belirlenemedi.";

                        break;
                    }
            }
        }
        private void RefreshGamepadDeviceList(
            string? selectedDeviceId)
        {
            IReadOnlyList<GamepadDeviceInfo> devices =
                _gamepadDeviceProvider
                    .GetConnectedDevices();

            GamepadDevices.Clear();

            /*
             * ActiveDeviceId == null mevcut backend semantiğinde
             * geçerli bir değerdir.
             *
             * UI bu değeri değiştirmeden taşıyabilmelidir.
             */
            GamepadDevices.Add(
                new GamepadDeviceOptionViewModel(
                    null,
                    "Varsayılan cihaz seçimi",
                    true));

            foreach (
                GamepadDeviceInfo device
                in devices
                    .OrderBy(
                        device =>
                            device.DisplayName,
                        StringComparer.CurrentCultureIgnoreCase))
            {
                GamepadDevices.Add(
                    new GamepadDeviceOptionViewModel(
                        device.Id,
                        device.DisplayName,
                        true));
            }

            if (!string.IsNullOrWhiteSpace(
                    selectedDeviceId) &&
                !devices.Any(
                    device =>
                        string.Equals(
                            device.Id,
                            selectedDeviceId,
                            StringComparison.Ordinal)))
            {
                /*
                 * Persist edilmiş cihaz şu anda bağlı değilse dropdown'dan
                 * sessizce kaybetmiyoruz.
                 */
                GamepadDevices.Add(
                    new GamepadDeviceOptionViewModel(
                        selectedDeviceId,
                        $"{selectedDeviceId} (bağlı değil)",
                        false));
            }

            SelectedGamepadDeviceId =
                selectedDeviceId;
        }

        private void LoadGamepadBindings(
            GamepadSettings settings)
        {
            GamepadBindings.Clear();

            AddGamepadBindingEditor(
                settings,
                GamepadActionIds.MotionPrecision,
                "Hassas Hareket");

            AddGamepadBindingEditor(
                settings,
                GamepadActionIds.WeaponFire,
                "Fire");

            AddGamepadBindingEditor(
                settings,
                GamepadActionIds.WeaponArmToggle,
                "ARM Aç / Kapat");

            AddGamepadBindingEditor(
                settings,
                GamepadActionIds.ModeManual,
                "MANUAL Mod");

            AddGamepadBindingEditor(
                settings,
                GamepadActionIds.ModeIdle,
                "IDLE Mod");

            AddGamepadBindingEditor(
                settings,
                GamepadActionIds.ModeEmergencyStop,
                "Acil Durdurma");
        }

        private void AddGamepadBindingEditor(
            GamepadSettings settings,
            string actionId,
            string displayName)
        {
            GamepadBindingSettings? binding =
                settings.Bindings
                    .FirstOrDefault(
                        candidate =>
                            string.Equals(
                                candidate.ActionId,
                                actionId,
                                StringComparison.Ordinal));

            if (binding is null)
            {
                /*
                 * Backend validator eksik action'ı zorunlu tutmuyor.
                 * Ancak Settings UI sabit altı desteklenen action'ı
                 * düzenler.
                 *
                 * Eksik binding geçersiz bir None değeriyle sessizce
                 * oluşturulmaz; backend default'u kullanılır.
                 */
                GamepadSettings defaults =
                    new();

                binding =
                    defaults.Bindings
                        .Single(
                            candidate =>
                                string.Equals(
                                    candidate.ActionId,
                                    actionId,
                                    StringComparison.Ordinal));
            }

            GamepadBindingEditorItemViewModel editor =
                new(
                    actionId,
                    displayName,
                    binding.Control,
                    _availableGamepadControls);

            GamepadBindings.Add(
                editor);
        }

        private void CancelShortcutCapture()
        {
            _keyboardShortcutService.CancelCapture();

            foreach (
                ShortcutEditorItemViewModel shortcut
                in Shortcuts)
            {
                shortcut.IsCapturing =
                    false;
            }
        }

        private void CancelGamepadCapture()
        {
            bool captureWasActive =
                _activeGamepadCaptureEditor is not null ||
                _gamepadControlCaptureService
                    .GetSnapshot()
                    .IsActive;

            if (!captureWasActive)
            {
                return;
            }

            checked
            {
                _gamepadCaptureGeneration++;
            }

            _gamepadControlCaptureService.Cancel();

            foreach (
                GamepadBindingEditorItemViewModel binding
                in GamepadBindings)
            {
                binding.IsCapturing =
                    false;
            }

            _activeGamepadCaptureEditor =
                null;
        }

        private void CancelAllInputCaptures()
        {
            CancelShortcutCapture();

            CancelGamepadCapture();
        }

        // ================================================================
        // CREATE NETWORK SETTINGS
        // ================================================================

        private NetworkSettings CreateNetworkSettings()
        {
            return new NetworkSettings
            {
                ServerIp =
                    ServerIp.Trim(),

                LocalBindIp =
                    LocalBindIp.Trim(),

                Command =
                    new()
                    {
                        RemotePort =
                            CommandRemotePort,

                        LocalPort =
                            CommandLocalPort,

                        ResponseTimeoutMs =
                            CommandResponseTimeoutMs
                    },

                Video =
                    new()
                    {
                        ListenPort =
                            VideoListenPort,

                        ReceiveBufferBytes =
                            VideoReceiveBufferBytes
                    },

                Detection =
                    new()
                    {
                        ListenPort =
                            DetectionListenPort,

                        StaleTimeoutMs =
                            DetectionStaleTimeoutMs
                    }
            };
        }

        // ================================================================
        // CREATE INPUT SETTINGS
        // ================================================================

        private InputSettings CreateInputSettings()
        {
            InputSettings settings =
                new()
                {
                    Gamepad =
                        CreateGamepadSettings()
                };

            foreach (
                ShortcutEditorItemViewModel shortcut
                in Shortcuts)
            {
                ShortcutBindingSettings? binding =
                    shortcut.ToSettings();

                if (binding is null)
                {
                    continue;
                }

                settings.Shortcuts.Add(
                    binding);
            }

            return settings;
        }

        private GamepadSettings CreateGamepadSettings()
        {
            return new GamepadSettings
            {
                Enabled =
                    GamepadEnabled,

                ActiveDeviceId =
                    SelectedGamepadDeviceId,

                InvertY =
                    GamepadInvertY,

                Deadzone =
                    GamepadDeadzone,

                TriggerThreshold =
                    GamepadTriggerThreshold,

                TriggerDebounceSamples =
                    GamepadTriggerDebounceSamples,

                PollingIntervalMs =
                    GamepadPollingIntervalMs,

                FireMinimumIntervalMs =
                    GamepadFireMinimumIntervalMs,

                Bindings =
                    GamepadBindings
                        .Select(
                            editor =>
                                editor.ToSettings())
                        .ToList()
            };
        }
    }
}