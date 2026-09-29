using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Input;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Domain.System;
using Microsoft.Extensions.Logging;

namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadInputRuntime
    {
        private readonly IGamepadDeviceProvider
            _deviceProvider;

        private readonly IGamepadSettingsStore
            _settingsStore;

        private readonly GamepadControlCaptureService
            _controlCaptureService;

        private long
            _lastCaptureVersion;

        private readonly GamepadSessionDeviceSelector
            _sessionDeviceSelector;

        private readonly GamepadApplicationActivityState
            _applicationActivityState;

        private long
            _lastApplicationActivityVersion;

        private string?
            _activeDeviceName;

        private long
            _lastSettingsVersion;

        private readonly GamepadAnalogAxisProcessor
            _analogAxisProcessor;

        private readonly GamepadControlEdgeProcessor
            _controlEdgeProcessor;

        private readonly GamepadBindingResolver
            _bindingResolver;

        private readonly IGamepadActionDispatcher
            _actionDispatcher;

        private readonly IGamepadAnalogMotionController
            _analogMotionController;

        private readonly ISystemRuntimeState
            _systemRuntimeState;

        private readonly GamepadRuntimeState
            _runtimeState;

        private readonly ILogger<GamepadInputRuntime>
            _logger;

        private string? _activeDeviceId;

        private bool _buttonNeutralRequired =
            true;

        private bool _analogResetRequired;

        private bool _modeInitialized;

        private SystemOperatingMode? _lastObservedMode;

        public GamepadInputRuntime(
    IGamepadDeviceProvider deviceProvider,
    IGamepadSettingsStore settingsStore,
    GamepadDeviceSelectionState deviceSelectionState,
    GamepadAnalogAxisProcessor analogAxisProcessor,
    GamepadControlEdgeProcessor controlEdgeProcessor,
    GamepadBindingResolver bindingResolver,
    IGamepadActionDispatcher actionDispatcher,
    IGamepadAnalogMotionController analogMotionController,
    ISystemRuntimeState systemRuntimeState,
    GamepadRuntimeState runtimeState,
    ILogger<GamepadInputRuntime> logger)
    : this(
        deviceProvider,
        settingsStore,
        deviceSelectionState,
        analogAxisProcessor,
        controlEdgeProcessor,
        bindingResolver,
        actionDispatcher,
        analogMotionController,
        systemRuntimeState,
        runtimeState,
        new GamepadControlCaptureService(),
        new GamepadApplicationActivityState(),
        logger)
        {
        }

        public GamepadInputRuntime(
    IGamepadDeviceProvider deviceProvider,
    IGamepadSettingsStore settingsStore,
    GamepadDeviceSelectionState deviceSelectionState,
    GamepadAnalogAxisProcessor analogAxisProcessor,
    GamepadControlEdgeProcessor controlEdgeProcessor,
    GamepadBindingResolver bindingResolver,
    IGamepadActionDispatcher actionDispatcher,
    IGamepadAnalogMotionController analogMotionController,
    ISystemRuntimeState systemRuntimeState,
    GamepadRuntimeState runtimeState,
    GamepadControlCaptureService controlCaptureService,
    GamepadApplicationActivityState applicationActivityState,
    ILogger<GamepadInputRuntime> logger)
        {
            _deviceProvider =
                deviceProvider;

            _settingsStore =
                settingsStore;

            _sessionDeviceSelector =
                new GamepadSessionDeviceSelector(
                    deviceSelectionState);

            _analogAxisProcessor =
                analogAxisProcessor;

            _controlEdgeProcessor =
                controlEdgeProcessor;

            _bindingResolver =
                bindingResolver;

            _actionDispatcher =
                actionDispatcher;

            _analogMotionController =
                analogMotionController;

            _systemRuntimeState =
                systemRuntimeState;

            _runtimeState =
                runtimeState;

            _controlCaptureService =
                controlCaptureService;

            _applicationActivityState =
                applicationActivityState;

            _logger =
                logger;
        }

        public async Task ProcessOnceAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                GamepadSettingsSnapshot settingsSnapshot = _settingsStore.GetSnapshot();

                GamepadControlCaptureSnapshot captureSnapshot = _controlCaptureService.GetSnapshot();

                if (_lastCaptureVersion !=
                    captureSnapshot.Version)
                {
                    MarkFailSafeResetRequired();

                    _lastCaptureVersion =
                        captureSnapshot.Version;
                }

                GamepadSettings settings =
                    settingsSnapshot.Settings;

                if (_lastSettingsVersion != 0 &&
                    _lastSettingsVersion !=
                        settingsSnapshot.Version)
                {

                    MarkFailSafeResetRequired();
                }

                _lastSettingsVersion =
                    settingsSnapshot.Version;

                if (!settings.Enabled)
                {
                    bool resetSucceeded =
                        await DetachActiveDeviceAsync(
                            cancellationToken);

                    _runtimeState.Update(
                        resetSucceeded
                            ? GamepadRuntimeStatus.Disabled
                            : GamepadRuntimeStatus.Error,
                        lastError:
                            resetSucceeded
                                ? null
                                : "Gamepad analog fail-safe reset tamamlanamadı.");

                    return;
                }

                GamepadApplicationActivitySnapshot applicationActivitySnapshot = _applicationActivityState.GetSnapshot();

                if (_lastApplicationActivityVersion !=
                    applicationActivitySnapshot.Version)
                {
                    /*
                     * Focus transition'da eski analog/button state hiçbir şekilde
                     * devam ettirilmez.
                     */
                    if (_activeDeviceId is not null)
                    {
                        MarkFailSafeResetRequired();
                    }
                    else
                    {
                        ResetLocalInputState();
                    }

                    _lastApplicationActivityVersion =
                        applicationActivitySnapshot.Version;
                }

                if (!applicationActivitySnapshot.IsActive)
                {
                    /*
                     * Uygulama focus dışında iken hiçbir gamepad hareketi veya
                     * button action'ı kabul edilmez.
                     */
                    if (!await EnsureAnalogResetAsync(
                            cancellationToken))
                    {
                        _runtimeState.Update(
                            GamepadRuntimeStatus.Error,
                            _activeDeviceId,
                            _activeDeviceName,
                            "Pencere focus kaybında gamepad fail-safe reset " +
                            "tamamlanamadı.");

                        return;
                    }

                    _runtimeState.Update(
                        _activeDeviceId is null
                            ? GamepadRuntimeStatus.Disconnected
                            : GamepadRuntimeStatus.NeutralRequired,
                        _activeDeviceId,
                        _activeDeviceName);

                    return;
                }

                IReadOnlyList<GamepadDeviceInfo> devices =
                    _deviceProvider.GetConnectedDevices();

                string? selectedDeviceId = _sessionDeviceSelector.ResolveActiveDeviceId(settings, devices);

                if (selectedDeviceId is null)
                {
                    await HandleNoActiveDeviceAsync(
                        cancellationToken);

                    return;
                }

                GamepadDeviceInfo? selectedDevice =
                    devices.FirstOrDefault(
                        device =>
                            string.Equals(
                                device.Id,
                                selectedDeviceId,
                                StringComparison.Ordinal));

                if (selectedDevice is null)
                {
                    await HandleNoActiveDeviceAsync(
                        cancellationToken);

                    return;
                }

                if (!string.Equals(
                        _activeDeviceId,
                        selectedDevice.Id,
                        StringComparison.Ordinal))
                {
                    bool attached =
                        await AttachDeviceAsync(
                            selectedDevice,
                            cancellationToken);

                    if (!attached)
                    {
                        return;
                    }
                }

                if (!_deviceProvider.TryRead(
                        selectedDevice.Id,
                        out GamepadReadingSnapshot? reading) ||
                    reading is null)
                {
                    await HandleNoActiveDeviceAsync(
                        cancellationToken);

                    return;
                }

                bool modeStateReady =
                    await HandleModeTransitionAsync(
                        selectedDevice,
                        cancellationToken);

                if (!modeStateReady)
                {
                    return;
                }

                /*
                 * Önceki reset başarısız kaldıysa hiçbir yeni gamepad
                 * action kabul edilmeden tekrar denenir.
                 */
                if (!await EnsureAnalogResetAsync(
                        cancellationToken))
                {
                    _runtimeState.Update(
                        GamepadRuntimeStatus.Error,
                        selectedDevice.Id,
                        selectedDevice.DisplayName,
                        "Gamepad analog fail-safe reset tamamlanamadı.");

                    return;
                }

                captureSnapshot = _controlCaptureService.GetSnapshot();

                if (_lastCaptureVersion !=
                    captureSnapshot.Version)
                {
                    MarkFailSafeResetRequired();

                    _lastCaptureVersion =
                        captureSnapshot.Version;

                    if (!await EnsureAnalogResetAsync(
                            cancellationToken))
                    {
                        _runtimeState.Update(
                            GamepadRuntimeStatus.Error,
                            selectedDevice.Id,
                            selectedDevice.DisplayName,
                            "Gamepad capture başlangıcında analog fail-safe " +
                            "reset tamamlanamadı.");

                        return;
                    }
                }

                if (captureSnapshot.IsActive)
                {
                    _controlCaptureService.ProcessReading(
                        selectedDevice.Id,
                        reading,
                        settings);

                    _runtimeState.Update(
                        GamepadRuntimeStatus.NeutralRequired,
                        selectedDevice.Id,
                        selectedDevice.DisplayName);

                    return;
                }

                GamepadAnalogState analogState =
                    _analogAxisProcessor.Process(
                        reading,
                        settings);

                UpdateButtonNeutralRequirement(
                    reading,
                    settings);

                bool gamepadReady =
                    analogState.IsReady &&
                    !_buttonNeutralRequired;

                if (gamepadReady &&
                    _systemRuntimeState.CurrentMode ==
                        SystemOperatingMode.Manual)
                {
                    try
                    {
                        bool motionAccepted =
                            await _analogMotionController.ApplyAsync(
                                analogState,
                                cancellationToken);

                        if (!motionAccepted)
                        {
                            MarkFailSafeResetRequired();

                            await EnsureAnalogResetAsync(
                                cancellationToken);

                            _runtimeState.Update(
                                GamepadRuntimeStatus.NeutralRequired,
                                selectedDevice.Id,
                                selectedDevice.DisplayName);

                            return;
                        }
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Gamepad analog hareket SET işlemi başarısız.");

                        MarkFailSafeResetRequired();

                        await EnsureAnalogResetAsync(
                            cancellationToken);

                        _runtimeState.Update(
                            GamepadRuntimeStatus.Error,
                            selectedDevice.Id,
                            selectedDevice.DisplayName,
                            exception.Message);

                        return;
                    }
                }

                /*
                 * Mode/ARM/Fire gibi button action'ları MANUAL hareket
                 * gating'inden bağımsızdır.
                 *
                 * Böylece IDLE durumundayken START ile MANUAL moda
                 * geçmek mümkündür.
                 */
                if (gamepadReady)
                {
                    GamepadControlTransitions transitions =
                        _controlEdgeProcessor.Process(
                            reading,
                            settings);

                    await DispatchTransitionsAsync(
                        transitions,
                        settings,
                        cancellationToken);
                }
                else
                {
                    _controlEdgeProcessor.Reset();
                }

                _runtimeState.Update(
                    gamepadReady
                        ? GamepadRuntimeStatus.Ready
                        : GamepadRuntimeStatus.NeutralRequired,
                    selectedDevice.Id,
                    selectedDevice.DisplayName);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Gamepad input çevrimi başarısız.");

                await HandleRuntimeFailureAsync(
                    exception,
                    cancellationToken);
            }
        }

        public async Task StopAsync(
            CancellationToken cancellationToken = default)
        {
            bool resetSucceeded =
                await DetachActiveDeviceAsync(
                    cancellationToken);

            _runtimeState.Update(
                resetSucceeded
                    ? GamepadRuntimeStatus.Disabled
                    : GamepadRuntimeStatus.Error,
                lastError:
                    resetSucceeded
                        ? null
                        : "Gamepad shutdown fail-safe reset tamamlanamadı.");
        }

        private async Task<bool> AttachDeviceAsync(
            GamepadDeviceInfo device,
            CancellationToken cancellationToken)
        {
            _activeDeviceId =
                device.Id;

            _activeDeviceName =
                device.DisplayName;

            _modeInitialized =
                true;

            _lastObservedMode =
                _systemRuntimeState.CurrentMode;

            MarkFailSafeResetRequired();

            bool resetSucceeded =
                await EnsureAnalogResetAsync(
                    cancellationToken);

            if (!resetSucceeded)
            {
                _runtimeState.Update(
                    GamepadRuntimeStatus.Error,
                    device.Id,
                    device.DisplayName,
                    "Gamepad bağlantı fail-safe reset işlemi başarısız.");

                return false;
            }

            _runtimeState.Update(
                GamepadRuntimeStatus.NeutralRequired,
                device.Id,
                device.DisplayName);

            _logger.LogInformation(
                "Gamepad aktif edildi. Device={DeviceName}, Id={DeviceId}",
                device.DisplayName,
                device.Id);

            return true;
        }

        private async Task HandleNoActiveDeviceAsync(
            CancellationToken cancellationToken)
        {
            if (_activeDeviceId is not null)
            {
                _logger.LogInformation(
                    "Aktif gamepad artık kullanılamıyor. DeviceId={DeviceId}",
                    _activeDeviceId);
            }

            bool resetSucceeded =
                await DetachActiveDeviceAsync(
                    cancellationToken);

            _runtimeState.Update(
                resetSucceeded
                    ? GamepadRuntimeStatus.Disconnected
                    : GamepadRuntimeStatus.Error,
                lastError:
                    resetSucceeded
                        ? null
                        : "Gamepad bağlantısı kesildi ancak analog " +
                          "fail-safe reset tamamlanamadı.");
        }

        private async Task HandleRuntimeFailureAsync(
            Exception exception,
            CancellationToken cancellationToken)
        {
            string? activeDeviceId =
                _activeDeviceId;

            _activeDeviceId =
                null;

            _modeInitialized =
                false;

            MarkFailSafeResetRequired();

            bool resetSucceeded =
                await EnsureAnalogResetAsync(
                    cancellationToken);

            _runtimeState.Update(
                GamepadRuntimeStatus.Error,
                activeDeviceId,
                null,
                resetSucceeded
                    ? exception.Message
                    : $"{exception.Message} | Analog fail-safe reset başarısız.");
        }

        private async Task<bool> DetachActiveDeviceAsync(
            CancellationToken cancellationToken)
        {
            if (_activeDeviceId is not null)
            {
                MarkFailSafeResetRequired();
            }

            bool resetSucceeded =
                await EnsureAnalogResetAsync(
                    cancellationToken);

            _activeDeviceId =
                null;

            _activeDeviceName =
                null;

            _modeInitialized =
                false;

            _lastObservedMode =
                null;

            ResetLocalInputState();

            return resetSucceeded;
        }

        private async Task<bool> HandleModeTransitionAsync(
            GamepadDeviceInfo device,
            CancellationToken cancellationToken)
        {
            SystemOperatingMode? currentMode =
                _systemRuntimeState.CurrentMode;

            if (!_modeInitialized)
            {
                _modeInitialized =
                    true;

                _lastObservedMode =
                    currentMode;

                return true;
            }

            if (_lastObservedMode ==
                currentMode)
            {
                return true;
            }

            _logger.LogInformation(
                "Gamepad çalışma modu değişikliği algıladı. " +
                "Previous={PreviousMode}, Current={CurrentMode}",
                _lastObservedMode,
                currentMode);

            _lastObservedMode =
                currentMode;

            /*
             * Her mode transition analog output'u sıfırlar ve stick /
             * button neutral re-arm ister.
             */
            MarkFailSafeResetRequired();

            bool resetSucceeded =
                await EnsureAnalogResetAsync(
                    cancellationToken);

            if (!resetSucceeded)
            {
                _runtimeState.Update(
                    GamepadRuntimeStatus.Error,
                    device.Id,
                    device.DisplayName,
                    "Mode değişiminde analog fail-safe reset başarısız.");

                return false;
            }

            return true;
        }

        private void MarkFailSafeResetRequired()
        {
            _analogResetRequired =
                true;

            ResetLocalInputState();
        }

        private async Task<bool> EnsureAnalogResetAsync(
            CancellationToken cancellationToken)
        {
            if (!_analogResetRequired)
            {
                return true;
            }

            try
            {
                await _analogMotionController.ResetAsync(
                    cancellationToken);

                _analogResetRequired =
                    false;

                return true;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Gamepad analog fail-safe reset işlemi başarısız.");

                /*
                 * Flag açık bırakılır.
                 * Bir sonraki cycle yeni action kabul etmeden tekrar dener.
                 */
                _analogResetRequired =
                    true;

                return false;
            }
        }

        private void ResetLocalInputState()
        {
            _analogAxisProcessor.Reset();

            _controlEdgeProcessor.Reset();

            _buttonNeutralRequired =
                true;
        }

        private void UpdateButtonNeutralRequirement(GamepadReadingSnapshot reading, GamepadSettings settings)
        {
            if (!_buttonNeutralRequired)
            {
                return;
            }

            if (!AreBoundControlsNeutral(
                    reading,
                    settings))
            {
                _controlEdgeProcessor.Reset();

                return;
            }

            _buttonNeutralRequired =
                false;

            _controlEdgeProcessor.Reset();
        }

        private bool AreBoundControlsNeutral(GamepadReadingSnapshot reading, GamepadSettings settings)
        {
            foreach (
                GamepadBindingSettings binding
                in settings.Bindings)
            {
                if (IsControlActive(
                        binding.Control,
                        reading,
                        settings))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsControlActive(GamepadControl control, GamepadReadingSnapshot reading, GamepadSettings settings)
        {
            return control switch
            {
                GamepadControl.RightTrigger =>
                    reading.RightTrigger >=
                    settings.TriggerThreshold,

                GamepadControl.LeftTrigger =>
                    reading.LeftTrigger >=
                    settings.TriggerThreshold,

                GamepadControl.None =>
                    false,

                _ =>
                    reading.PressedControls.Contains(
                        control)
            };
        }

        private async Task DispatchTransitionsAsync(
            GamepadControlTransitions transitions,
            GamepadSettings settings,
            CancellationToken cancellationToken)
        {
            foreach (
                GamepadControl control
                in transitions.Pressed)
            {
                if (!_bindingResolver.TryResolve(
                        settings,
                        control,
                        out string? actionId) ||
                    actionId is null)
                {
                    continue;
                }

                try
                {
                    await _actionDispatcher.HandlePressedAsync(
                        actionId,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Gamepad action uygulanamadı. " +
                        "Action={ActionId}, Control={Control}",
                        actionId,
                        control);
                }
            }

            foreach (
                GamepadControl control
                in transitions.Released)
            {
                if (!_bindingResolver.TryResolve(
                        settings,
                        control,
                        out string? actionId) ||
                    actionId is null)
                {
                    continue;
                }

                try
                {
                    await _actionDispatcher.HandleReleasedAsync(
                        actionId,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Gamepad action release uygulanamadı. " +
                        "Action={ActionId}, Control={Control}",
                        actionId,
                        control);
                }
            }
        }
    }
}