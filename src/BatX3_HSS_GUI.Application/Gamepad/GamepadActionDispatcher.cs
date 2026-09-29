using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;
using System.Globalization;

namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadActionDispatcher :
           IGamepadActionDispatcher
    {
        private readonly IApplicationActionService
            _applicationActionService;

        private readonly ISystemModeService
            _systemModeService;

        private readonly IParameterService
            _parameterService;

        private readonly IGamepadAnalogMotionController
            _analogMotionController;

        private readonly IGamepadSettingsStore
            _settingsStore;

        private readonly TimeProvider
            _timeProvider;

        private long? _lastFireTimestamp;

        public GamepadActionDispatcher(
            IApplicationActionService applicationActionService,
            ISystemModeService systemModeService,
            IParameterService parameterService,
            IGamepadAnalogMotionController analogMotionController,
            IGamepadSettingsStore settingsStore,
            TimeProvider timeProvider)
        {
            _applicationActionService =
                applicationActionService;

            _systemModeService =
                systemModeService;

            _parameterService =
                parameterService;

            _analogMotionController =
                analogMotionController;

            _settingsStore =
                settingsStore;

            _timeProvider =
                timeProvider;
        }

        public async Task HandlePressedAsync(
            string actionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                actionId);

            switch (actionId)
            {
                case GamepadActionIds.WeaponFire:
                    {
                        await FireAsync(
                            cancellationToken);

                        break;
                    }

                case GamepadActionIds.WeaponArmToggle:
                    {
                        await ToggleArmAsync(
                            cancellationToken);

                        break;
                    }

                case GamepadActionIds.ModeManual:
                    {
                        await _systemModeService.SetModeAsync(
                            SystemOperatingMode.Manual);

                        break;
                    }

                case GamepadActionIds.ModeIdle:
                    {
                        await _systemModeService.SetModeAsync(
                            SystemOperatingMode.Idle);

                        break;
                    }

                case GamepadActionIds.ModeEmergencyStop:
                    {
                        await _systemModeService.SetModeAsync(
                            SystemOperatingMode.EmergencyStop);

                        break;
                    }

                case GamepadActionIds.MotionPrecision:
                    {
                        await _analogMotionController
                            .SetPrecisionAsync(
                                true,
                                cancellationToken);

                        break;
                    }

                default:
                    {
                        throw new InvalidOperationException(
                            $"Desteklenmeyen gamepad action: {actionId}");
                    }
            }
        }

        public async Task HandleReleasedAsync(
            string actionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                actionId);

            if (string.Equals(
                    actionId,
                    GamepadActionIds.MotionPrecision,
                    StringComparison.Ordinal))
            {
                await _analogMotionController
                    .SetPrecisionAsync(
                        false,
                        cancellationToken);
            }
        }

        private async Task FireAsync(
            CancellationToken cancellationToken)
        {
            long now =
                _timeProvider.GetTimestamp();

            GamepadSettings settings =
                _settingsStore
                    .GetSnapshot()
                    .Settings;

            if (_lastFireTimestamp.HasValue)
            {
                TimeSpan elapsed =
                    _timeProvider.GetElapsedTime(
                        _lastFireTimestamp.Value,
                        now);

                TimeSpan minimumInterval =
                    TimeSpan.FromMilliseconds(
                        Math.Max(
                            0,
                            settings.FireMinimumIntervalMs));

                if (elapsed <
                    minimumInterval)
                {
                    return;
                }
            }

            _lastFireTimestamp =
                now;

            await _applicationActionService.PressAsync(
                GamepadActionIds.WeaponFire,
                cancellationToken);
        }

        private async Task ToggleArmAsync(
            CancellationToken cancellationToken)
        {
            ParameterOperationResult readResult =
                await _parameterService.GetAsync(
                    ParameterNames.Weapon.Armed,
                    cancellationToken);

            if (!readResult.IsSuccess ||
                readResult.Value is null)
            {
                throw new InvalidOperationException(
                    "Silah kurma durumu doğrulanamadığı için " +
                    "gamepad ARM işlemi uygulanmadı.");
            }

            int currentValue;

            try
            {
                currentValue =
                    Convert.ToInt32(
                        readResult.Value,
                        CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
                when (exception is
                    FormatException or
                    InvalidCastException or
                    OverflowException)
            {
                throw new InvalidOperationException(
                    "Silah kurma durumu geçerli bir değer değil.",
                    exception);
            }

            if (currentValue is not
                (0 or 1))
            {
                throw new InvalidOperationException(
                    $"Beklenmeyen weapon.armed değeri: {currentValue}");
            }

            int targetValue =
                currentValue == 0
                    ? 1
                    : 0;

            ParameterOperationResult writeResult =
                await _parameterService.SetAsync(
                    ParameterNames.Weapon.Armed,
                    targetValue,
                    cancellationToken);

            if (!writeResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Gamepad ARM işlemi reddedildi: " +
                    $"{writeResult.Status}");
            }
        }
    }
}