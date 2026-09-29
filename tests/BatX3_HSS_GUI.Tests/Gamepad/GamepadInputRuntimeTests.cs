using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Application.Input;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Domain.System;
using Microsoft.Extensions.Logging.Abstractions;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadInputRuntimeTests
    {
        [Fact]
        public async Task DisplacedStickOnConnection_ShouldRequireNeutral()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        1.0
                });

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            /*
             * Cihaz bağlanırken fail-safe reset yapılır.
             */
            Assert.Equal(
                1,
                analogController.ResetCount);

            /*
             * Stick merkezde olmadığı için analog movement
             * uygulanmamalıdır.
             */
            Assert.Empty(
                analogController.ApplyRequests);

            Assert.Equal(
                GamepadRuntimeStatus.NeutralRequired,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task NeutralThenAnalogMovement_ShouldApplyAnalogPan()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            /*
             * İlk neutral sample re-arm işlemini tamamlar.
             */
            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        1.0
                });

            await runtime.ProcessOnceAsync();

            Assert.NotEmpty(
                analogController.ApplyRequests);

            GamepadAnalogState lastRequest =
                analogController.ApplyRequests[^1];

            Assert.True(
                lastRequest.IsReady);

            Assert.Equal(
                1.0,
                lastRequest.Pan,
                precision: 6);

            Assert.Equal(
                0.0,
                lastRequest.Tilt,
                precision: 6);

            Assert.Equal(
                GamepadRuntimeStatus.Ready,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task AnalogMovementOutsideManualMode_ShouldNotBeApplied()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Idle);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            /*
             * Neutral re-arm.
             */
            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        1.0
                });

            await runtime.ProcessOnceAsync();

            /*
             * IDLE modunda analog movement controller'a
             * aktarılmamalıdır.
             */
            Assert.Empty(
                analogController.ApplyRequests);

            /*
             * Gamepad fiziksel olarak çalışır ve neutral gate
             * tamamlanmıştır; yalnız movement mode-gated'dir.
             */
            Assert.Equal(
                GamepadRuntimeStatus.Ready,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task Disconnect_ShouldResetAnalogOutputs()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            Assert.Equal(
                1,
                analogController.ResetCount);

            provider.Disconnect(
                "A");

            await runtime.ProcessOnceAsync();

            /*
             * Bir reset attach,
             * ikinci reset disconnect fail-safe içindir.
             */
            Assert.Equal(
                2,
                analogController.ResetCount);

            Assert.Equal(
                GamepadRuntimeStatus.Disconnected,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task ModeTransition_ShouldResetAnalogOutputsAndRequireNeutral()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            /*
             * Neutral re-arm.
             */
            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        1.0
                });

            await runtime.ProcessOnceAsync();

            Assert.NotEmpty(
                analogController.ApplyRequests);

            int applyCountBeforeModeChange =
                analogController.ApplyRequests.Count;

            Assert.Equal(
                1,
                analogController.ResetCount);

            /*
             * MANUAL -> IDLE.
             *
             * Stick hâlâ merkezde değil.
             */
            systemRuntimeState.UpdateMode(
                SystemOperatingMode.Idle);

            await runtime.ProcessOnceAsync();

            /*
             * Mode transition fail-safe reset üretmelidir.
             */
            Assert.Equal(
                2,
                analogController.ResetCount);

            /*
             * Stick displaced olduğu için transition sonrasında
             * yeniden movement uygulanmamalıdır.
             */
            Assert.Equal(
                applyCountBeforeModeChange,
                analogController.ApplyRequests.Count);

            Assert.Equal(
                GamepadRuntimeStatus.NeutralRequired,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task ModeTransition_WithNeutralStick_ShouldRearmWithoutMovement()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            int resetCountBeforeModeChange =
                analogController.ResetCount;

            systemRuntimeState.UpdateMode(
                SystemOperatingMode.Idle);

            await runtime.ProcessOnceAsync();

            Assert.Equal(
                resetCountBeforeModeChange + 1,
                analogController.ResetCount);

            Assert.Equal(
                GamepadRuntimeStatus.Ready,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task Stop_ShouldPerformFailSafeAnalogReset()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            Assert.Equal(
                1,
                analogController.ResetCount);

            await runtime.StopAsync();

            Assert.Equal(
                2,
                analogController.ResetCount);

            Assert.Equal(
                GamepadRuntimeStatus.Disabled,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task ResetFailureOnConnection_ShouldBlockGamepadInput()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new()
                {
                    FailReset =
                        true
                };

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            Assert.Equal(
                1,
                analogController.ResetCount);

            Assert.Empty(
                analogController.ApplyRequests);

            Assert.Empty(
                dispatcher.PressedActions);

            Assert.Equal(
                GamepadRuntimeStatus.Error,
                runtimeState.GetSnapshot().Status);
        }

        [Fact]
        public async Task HeldFireButton_ShouldDispatchOnlyOnePressedAction()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            /*
             * Neutral re-arm.
             */
            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.RightShoulder
                        }
                });

            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();

            Assert.Single(
                dispatcher.PressedActions);

            Assert.Equal(
                GamepadActionIds.WeaponFire,
                dispatcher.PressedActions[0]);
        }

        [Fact]
        public async Task FireHeldDuringConnection_ShouldRequireNeutralBeforeFire()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.RightShoulder
                        }
                });

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();

            Assert.Empty(
                dispatcher.PressedActions);

            Assert.Equal(
                GamepadRuntimeStatus.NeutralRequired,
                runtimeState.GetSnapshot().Status);

            /*
             * Önce bütün bound control'ler neutral olmalı.
             */
            provider.SetReading(
                "A",
                new GamepadReadingSnapshot());

            await runtime.ProcessOnceAsync();

            Assert.Empty(
                dispatcher.PressedActions);

            /*
             * Neutral sonrasında yeni fiziksel basış.
             */
            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.RightShoulder
                        }
                });

            await runtime.ProcessOnceAsync();

            Assert.Single(
                dispatcher.PressedActions);

            Assert.Equal(
                GamepadActionIds.WeaponFire,
                dispatcher.PressedActions[0]);
        }

        [Fact]
        public async Task HeldModeButton_ShouldDispatchOnlyOnePressedAction()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Idle);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.Menu
                        }
                });

            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();

            Assert.Single(
                dispatcher.PressedActions);

            Assert.Equal(
                GamepadActionIds.ModeManual,
                dispatcher.PressedActions[0]);
        }

        [Fact]
        public async Task RightTrigger_ShouldDispatchPrecisionPressAfterDebounce()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    RightTrigger =
                        1.0
                });

            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();

            Assert.DoesNotContain(
                GamepadActionIds.MotionPrecision,
                dispatcher.PressedActions);

            await runtime.ProcessOnceAsync();

            Assert.Contains(
                GamepadActionIds.MotionPrecision,
                dispatcher.PressedActions);
        }

        [Fact]
        public async Task RightTrigger_ShouldDispatchPrecisionReleaseAfterDebounce()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new();

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    RightTrigger =
                        1.0
                });

            /*
             * Press debounce.
             */
            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();

            Assert.Contains(
                GamepadActionIds.MotionPrecision,
                dispatcher.PressedActions);

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    RightTrigger =
                        0.0
                });

            /*
             * Release debounce.
             */
            await runtime.ProcessOnceAsync();
            await runtime.ProcessOnceAsync();

            Assert.DoesNotContain(
                GamepadActionIds.MotionPrecision,
                dispatcher.ReleasedActions);

            await runtime.ProcessOnceAsync();

            Assert.Contains(
                GamepadActionIds.MotionPrecision,
                dispatcher.ReleasedActions);
        }

        [Fact]
        public async Task ButtonActionFailure_ShouldNotMarkControllerAsDisconnected()
        {
            FakeGamepadDeviceProvider provider =
                new();

            provider.Connect(
                "A",
                "Pad A",
                new GamepadReadingSnapshot());

            RecordingGamepadActionDispatcher dispatcher =
                new()
                {
                    ThrowOnPress =
                        true
                };

            RecordingGamepadAnalogMotionController analogController =
                new();

            FakeSystemRuntimeState systemRuntimeState =
                new(
                    SystemOperatingMode.Manual);

            GamepadRuntimeState runtimeState =
                new();

            GamepadInputRuntime runtime =
                CreateRuntime(
                    provider,
                    dispatcher,
                    analogController,
                    systemRuntimeState,
                    runtimeState);

            await runtime.ProcessOnceAsync();

            provider.SetReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.RightShoulder
                        }
                });

            await runtime.ProcessOnceAsync();

            /*
             * Application command hatası controller hardware
             * lifecycle hatası değildir.
             */
            Assert.Equal(
                GamepadRuntimeStatus.Ready,
                runtimeState.GetSnapshot().Status);

            Assert.Single(
                dispatcher.PressedActions);
        }

        private static GamepadInputRuntime CreateRuntime(
            FakeGamepadDeviceProvider provider,
            RecordingGamepadActionDispatcher dispatcher,
            RecordingGamepadAnalogMotionController analogController,
            FakeSystemRuntimeState systemRuntimeState,
            GamepadRuntimeState runtimeState)
        {           

            GamepadSettings settings =
                new()
                {
                    InvertY =
                        false,

                    Deadzone =
                        0.15,

                    TriggerThreshold =
                        0.50,

                    TriggerDebounceSamples =
                        3
                };

            GamepadSettingsStore settingsStore = new(        settings);

            return new GamepadInputRuntime(
                provider,
                settingsStore,
                new GamepadDeviceSelectionState(),
                new GamepadAnalogAxisProcessor(),
                new GamepadControlEdgeProcessor(),
                new GamepadBindingResolver(),
                dispatcher,
                analogController,
                systemRuntimeState,
                runtimeState,
                NullLogger<GamepadInputRuntime>.Instance);
        }

        private sealed class FakeGamepadDeviceProvider :
            IGamepadDeviceProvider
        {
            private readonly Dictionary<
                string,
                FakeDevice> _devices =
                    new(
                        StringComparer.Ordinal);

            public void Connect(
                string id,
                string name,
                GamepadReadingSnapshot reading)
            {
                _devices[id] =
                    new FakeDevice(
                        new GamepadDeviceInfo(
                            id,
                            name),
                        reading);
            }

            public void Disconnect(
                string id)
            {
                _devices.Remove(
                    id);
            }

            public void SetReading(
                string id,
                GamepadReadingSnapshot reading)
            {
                FakeDevice device =
                    _devices[id];

                _devices[id] =
                    device with
                    {
                        Reading =
                            reading
                    };
            }

            public IReadOnlyList<GamepadDeviceInfo>
                GetConnectedDevices()
            {
                return _devices
                    .Values
                    .Select(
                        device =>
                            device.Info)
                    .ToArray();
            }

            public bool TryRead(
                string deviceId,
                out GamepadReadingSnapshot? snapshot)
            {
                if (!_devices.TryGetValue(
                        deviceId,
                        out FakeDevice? device))
                {
                    snapshot =
                        null;

                    return false;
                }

                snapshot =
                    device.Reading;

                return true;
            }

            private sealed record FakeDevice(
                GamepadDeviceInfo Info,
                GamepadReadingSnapshot Reading);
        }

        private sealed class RecordingGamepadActionDispatcher :
            IGamepadActionDispatcher
        {
            public List<string> PressedActions
            {
                get;
            } =
                new();

            public List<string> ReleasedActions
            {
                get;
            } =
                new();

            public bool ThrowOnPress
            {
                get;
                init;
            }

            public bool ThrowOnRelease
            {
                get;
                init;
            }

            public Task HandlePressedAsync(
                string actionId,
                CancellationToken cancellationToken = default)
            {
                PressedActions.Add(
                    actionId);

                if (ThrowOnPress)
                {
                    throw new InvalidOperationException(
                        "Test gamepad press failure.");
                }

                return Task.CompletedTask;
            }

            public Task HandleReleasedAsync(
                string actionId,
                CancellationToken cancellationToken = default)
            {
                ReleasedActions.Add(
                    actionId);

                if (ThrowOnRelease)
                {
                    throw new InvalidOperationException(
                        "Test gamepad release failure.");
                }

                return Task.CompletedTask;
            }
        }

        private sealed class RecordingGamepadAnalogMotionController :
            IGamepadAnalogMotionController
        {
            public List<GamepadAnalogState> ApplyRequests
            {
                get;
            } =
                new();

            public List<bool> PrecisionRequests
            {
                get;
            } =
                new();

            public int ResetCount
            {
                get;
                private set;
            }

            public bool ApplyResult
            {
                get;
                set;
            } =
                true;

            public bool PrecisionResult
            {
                get;
                set;
            } =
                true;

            public bool FailReset
            {
                get;
                set;
            }

            public Task<bool> ApplyAsync(
                GamepadAnalogState state,
                CancellationToken cancellationToken = default)
            {
                ApplyRequests.Add(
                    state);

                return Task.FromResult(
                    ApplyResult);
            }

            public Task<bool> SetPrecisionAsync(
                bool isActive,
                CancellationToken cancellationToken = default)
            {
                PrecisionRequests.Add(
                    isActive);

                return Task.FromResult(
                    PrecisionResult);
            }

            public Task ResetAsync(
                CancellationToken cancellationToken = default)
            {
                ResetCount++;

                if (FailReset)
                {
                    throw new InvalidOperationException(
                        "Test fail-safe reset failure.");
                }

                return Task.CompletedTask;
            }
        }

        private sealed class FakeSystemRuntimeState :
            ISystemRuntimeState
        {
            public FakeSystemRuntimeState(
                SystemOperatingMode? mode)
            {
                CurrentMode =
                    mode;
            }

            public SystemOperatingMode? CurrentMode
            {
                get;
                private set;
            }

            public void UpdateMode(
                SystemOperatingMode mode)
            {
                CurrentMode =
                    mode;
            }

            public void MarkUnknown()
            {
                CurrentMode =
                    null;
            }
        }
    }
}