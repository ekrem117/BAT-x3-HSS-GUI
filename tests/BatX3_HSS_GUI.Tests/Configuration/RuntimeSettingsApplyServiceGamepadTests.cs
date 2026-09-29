using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Configuration.Runtime;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Infrastructure.Configuration.Runtime;
using Microsoft.Extensions.Logging.Abstractions;

namespace BatX3_HSS_GUI.Tests.Configuration
{
    public sealed class
            RuntimeSettingsApplyServiceGamepadTests
    {
        [Fact]
        public async Task GamepadOnlyChange_ShouldPersistAndHotApply()
        {
            NetworkSettings networkSettings =
                new();

            InputSettings previousInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings
                        {
                            Deadzone =
                                0.15
                        }
                };

            RecordingSettingsService settingsService =
                new(
                    networkSettings,
                    previousInput);

            GamepadSettingsStore store =
                new(
                    previousInput.Gamepad);

            using RuntimeSettingsApplyService service =
                CreateService(
                    settingsService,
                    store);

            InputSettings targetInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings
                        {
                            Deadzone =
                                0.30
                        }
                };

            long previousVersion =
                store.GetSnapshot().Version;

            RuntimeSettingsApplyResult result =
                await service.ApplyAsync(
                    new NetworkSettings(),
                    targetInput);

            Assert.False(
                result.NetworkReconfigured);

            Assert.Equal(
                1,
                settingsService.SaveCount);

            Assert.Equal(
                0.30,
                settingsService.CurrentInput
                    .Gamepad
                    .Deadzone);

            Assert.Equal(
                0.30,
                store.GetSnapshot()
                    .Settings
                    .Deadzone);

            Assert.True(
                store.GetSnapshot().Version >
                previousVersion);
        }

        [Fact]
        public async Task EquivalentGamepadSettings_ShouldNotAdvanceRuntimeVersion()
        {
            NetworkSettings networkSettings =
                new();

            InputSettings previousInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings()
                };

            RecordingSettingsService settingsService =
                new(
                    networkSettings,
                    previousInput);

            GamepadSettingsStore store =
                new(
                    previousInput.Gamepad);

            using RuntimeSettingsApplyService service =
                CreateService(
                    settingsService,
                    store);

            long previousVersion =
                store.GetSnapshot().Version;

            InputSettings targetInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings(),

                    Shortcuts =
                    [
                        new ShortcutBindingSettings
                        {
                            ActionId =
                                "Weapon.Fire",

                            Key =
                                "F",

                            Modifiers =
                                ShortcutModifiers.None
                        }
                    ]
                };

            await service.ApplyAsync(
                new NetworkSettings(),
                targetInput);

            Assert.Equal(
                previousVersion,
                store.GetSnapshot().Version);
        }

        [Fact]
        public async Task InvalidGamepad_ShouldFailBeforePersistence()
        {
            NetworkSettings networkSettings =
                new();

            InputSettings previousInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings()
                };

            RecordingSettingsService settingsService =
                new(
                    networkSettings,
                    previousInput);

            GamepadSettingsStore store =
                new(
                    previousInput.Gamepad);

            using RuntimeSettingsApplyService service =
                CreateService(
                    settingsService,
                    store);

            long previousVersion =
                store.GetSnapshot().Version;

            InputSettings invalidInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings
                        {
                            Deadzone =
                                1.50
                        }
                };

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        service.ApplyAsync(
                            new NetworkSettings(),
                            invalidInput));

            Assert.Equal(
                0,
                settingsService.SaveCount);

            Assert.Equal(
                previousVersion,
                store.GetSnapshot().Version);
        }

        [Fact]
        public async Task RuntimeStoreFailure_ShouldRollbackPersistedInput()
        {
            NetworkSettings networkSettings =
                new();

            InputSettings previousInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings
                        {
                            Deadzone =
                                0.15
                        }
                };

            RecordingSettingsService settingsService =
                new(
                    networkSettings,
                    previousInput);

            FailOnceGamepadSettingsStore store =
                new(
                    previousInput.Gamepad);

            using RuntimeSettingsApplyService service =
                CreateService(
                    settingsService,
                    store);

            InputSettings targetInput =
                new()
                {
                    Gamepad =
                        new GamepadSettings
                        {
                            Deadzone =
                                0.30
                        }
                };

            RuntimeSettingsApplyException exception =
                await Assert.ThrowsAsync<
                    RuntimeSettingsApplyException>(
                        () =>
                            service.ApplyAsync(
                                new NetworkSettings(),
                                targetInput));

            Assert.True(
                exception.RollbackSucceeded);

            /*
             * İlk save target,
             * ikinci save rollback snapshot'ıdır.
             */
            Assert.Equal(
                2,
                settingsService.SaveCount);

            Assert.Equal(
                0.15,
                settingsService.CurrentInput
                    .Gamepad
                    .Deadzone);

            Assert.Equal(
                0.15,
                store.GetSnapshot()
                    .Settings
                    .Deadzone);
        }

        private static RuntimeSettingsApplyService
            CreateService(
                ISettingsService settingsService,
                IGamepadSettingsStore gamepadSettingsStore)
        {
            /*
             * Bu focused test yalnız no-network-rebind path'ini çalıştırır.
             * UDP dependencies bu execution path'inde dereference edilmez.
             */
            return new RuntimeSettingsApplyService(
                settingsService,
                commandService: null!,
                videoFrameSource: null!,
                detectionFrameSource: null!,
                frameSynchronizer: null!,
                gamepadSettingsStore,
                NullLogger<
                    RuntimeSettingsApplyService>.Instance);
        }

        private sealed class RecordingSettingsService :
            ISettingsService
        {
            public RecordingSettingsService(
                NetworkSettings networkSettings,
                InputSettings inputSettings)
            {
                CurrentNetwork =
                    networkSettings;

                CurrentInput =
                    inputSettings;
            }

            public NetworkSettings CurrentNetwork
            {
                get;
                private set;
            }

            public InputSettings CurrentInput
            {
                get;
                private set;
            }

            public int SaveCount
            {
                get;
                private set;
            }

            public NetworkSettings GetNetworkSettings()
            {
                return CurrentNetwork;
            }

            public InputSettings GetInputSettings()
            {
                return CurrentInput;
            }

            public VideoOverlaySettings GetVideoOverlaySettings()
            {
                return new VideoOverlaySettings();
            }

            public NetworkSettings GetDefaultNetworkSettings()
            {
                return CurrentNetwork;
            }

            public InputSettings GetDefaultInputSettings()
            {
                return CurrentInput;
            }

            public Task SaveSettingsAsync(
                NetworkSettings networkSettings,
                InputSettings inputSettings,
                CancellationToken cancellationToken = default)
            {
                SaveCount++;

                CurrentNetwork =
                    networkSettings;

                CurrentInput =
                    inputSettings;

                return Task.CompletedTask;
            }
        }
        private sealed class
            FailOnceGamepadSettingsStore :
                IGamepadSettingsStore
        {
            private GamepadSettingsSnapshot
                _snapshot;

            private bool
                _shouldFail =
                    true;

            public FailOnceGamepadSettingsStore(
                GamepadSettings initialSettings)
            {
                _snapshot =
                    new GamepadSettingsSnapshot(
                        1,
                        Clone(
                            initialSettings));
            }

            public GamepadSettingsSnapshot GetSnapshot()
            {
                return _snapshot;
            }

            public void Replace(
                GamepadSettings settings)
            {
                if (_shouldFail)
                {
                    _shouldFail =
                        false;

                    throw new InvalidOperationException(
                        "Test runtime store failure.");
                }

                _snapshot =
                    new GamepadSettingsSnapshot(
                        _snapshot.Version + 1,
                        Clone(
                            settings));
            }

            private static GamepadSettings Clone(
                GamepadSettings source)
            {
                return new GamepadSettings
                {
                    Enabled =
                        source.Enabled,

                    ActiveDeviceId =
                        source.ActiveDeviceId,

                    InvertY =
                        source.InvertY,

                    Deadzone =
                        source.Deadzone,

                    TriggerThreshold =
                        source.TriggerThreshold,

                    TriggerDebounceSamples =
                        source.TriggerDebounceSamples,

                    PollingIntervalMs =
                        source.PollingIntervalMs,

                    FireMinimumIntervalMs =
                        source.FireMinimumIntervalMs,

                    Bindings =
                        source.Bindings
                            .Select(
                                binding =>
                                    new GamepadBindingSettings
                                    {
                                        ActionId =
                                            binding.ActionId,

                                        Control =
                                            binding.Control
                                    })
                            .ToList()
                };
            }
        }
    }
}