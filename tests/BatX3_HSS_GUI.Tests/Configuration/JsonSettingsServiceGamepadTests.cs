using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Infrastructure.Configuration;

namespace BatX3_HSS_GUI.Tests.Configuration
{
    public sealed class JsonSettingsServiceGamepadTests :
           IDisposable
    {
        private readonly string _temporaryDirectory;

        private readonly string _userSettingsFilePath;

        public JsonSettingsServiceGamepadTests()
        {
            _temporaryDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "BatX3.Tests",
                    Guid.NewGuid().ToString(
                        "N"));

            Directory.CreateDirectory(
                _temporaryDirectory);

            _userSettingsFilePath =
                Path.Combine(
                    _temporaryDirectory,
                    "appsettings.user.json");
        }

        [Fact]
        public void LegacyInputWithoutGamepad_ShouldUseDefaultGamepad()
        {
            File.WriteAllText(
                _userSettingsFilePath,
                """
                {
                  "Input": {
                    "Shortcuts": [
                      {
                        "ActionId": "Weapon.Fire",
                        "Key": "F",
                        "Modifiers": "None"
                      }
                    ]
                  }
                }
                """);

            InputSettings defaults =
                CreateDefaultInputSettings();

            JsonSettingsService service =
                CreateService(
                    defaults);

            InputSettings result =
                service.GetInputSettings();

            Assert.Single(
                result.Shortcuts);

            Assert.Equal(
                "Weapon.Fire",
                result.Shortcuts[0].ActionId);

            Assert.Equal(
                defaults.Gamepad.Deadzone,
                result.Gamepad.Deadzone);

            Assert.Equal(
                defaults.Gamepad.PollingIntervalMs,
                result.Gamepad.PollingIntervalMs);

            Assert.Equal(
                defaults.Gamepad.Bindings.Count,
                result.Gamepad.Bindings.Count);
        }

        [Fact]
        public void ExplicitEmptyShortcuts_ShouldRemainEmpty()
        {
            File.WriteAllText(
                _userSettingsFilePath,
                """
                {
                  "Input": {
                    "Shortcuts": []
                  }
                }
                """);

            InputSettings defaults =
                CreateDefaultInputSettings();

            defaults.Shortcuts.Add(
                new ShortcutBindingSettings
                {
                    ActionId =
                        "Weapon.Fire",

                    Key =
                        "F",

                    Modifiers =
                        ShortcutModifiers.None
                });

            JsonSettingsService service =
                CreateService(
                    defaults);

            InputSettings result =
                service.GetInputSettings();

            Assert.Empty(
                result.Shortcuts);

            Assert.Equal(
                defaults.Gamepad.Deadzone,
                result.Gamepad.Deadzone);
        }

        [Fact]
        public void UserGamepad_ShouldOverrideDefaultGamepad()
        {
            File.WriteAllText(
                _userSettingsFilePath,
                """
                {
                  "Input": {
                    "Shortcuts": [],
                    "Gamepad": {
                      "Enabled": false,
                      "ActiveDeviceId": "TEST-PAD",
                      "InvertY": false,
                      "Deadzone": 0.25,
                      "TriggerThreshold": 0.6,
                      "TriggerDebounceSamples": 4,
                      "PollingIntervalMs": 20,
                      "FireMinimumIntervalMs": 1500,
                      "Bindings": [
                        {
                          "ActionId": "Weapon.Fire",
                          "Control": "RightShoulder"
                        }
                      ]
                    }
                  }
                }
                """);

            JsonSettingsService service =
                CreateService(
                    CreateDefaultInputSettings());

            InputSettings result =
                service.GetInputSettings();

            Assert.False(
                result.Gamepad.Enabled);

            Assert.Equal(
                "TEST-PAD",
                result.Gamepad.ActiveDeviceId);

            Assert.Equal(
                0.25,
                result.Gamepad.Deadzone);

            Assert.Equal(
                20,
                result.Gamepad.PollingIntervalMs);

            GamepadBindingSettings binding =
                Assert.Single(
                    result.Gamepad.Bindings);

            Assert.Equal(
                GamepadActionIds.WeaponFire,
                binding.ActionId);

            Assert.Equal(
                GamepadControl.RightShoulder,
                binding.Control);
        }

        [Fact]
        public async Task SaveSettings_ShouldPersistGamepadInsideInput()
        {
            InputSettings inputSettings =
                CreateDefaultInputSettings();

            inputSettings.Gamepad.Enabled =
                false;

            inputSettings.Gamepad.ActiveDeviceId =
                "TEST-PAD";

            JsonSettingsService service =
                CreateService(
                    inputSettings);

            await service.SaveSettingsAsync(
                new NetworkSettings(),
                inputSettings);

            string json =
                File.ReadAllText(
                    _userSettingsFilePath);

            Assert.Contains(
                "\"Gamepad\"",
                json,
                StringComparison.Ordinal);

            Assert.Contains(
                "\"ActiveDeviceId\": \"TEST-PAD\"",
                json,
                StringComparison.Ordinal);

            InputSettings reloaded =
                service.GetInputSettings();

            Assert.False(
                reloaded.Gamepad.Enabled);

            Assert.Equal(
                "TEST-PAD",
                reloaded.Gamepad.ActiveDeviceId);
        }

        private JsonSettingsService CreateService(
            InputSettings defaultInputSettings)
        {
            return new JsonSettingsService(
                new NetworkSettings(),
                defaultInputSettings,
                _userSettingsFilePath);
        }

        private static InputSettings
            CreateDefaultInputSettings()
        {
            return new InputSettings
            {
                Gamepad =
                    new GamepadSettings
                    {
                        Enabled =
                            true,

                        Deadzone =
                            0.20,

                        PollingIntervalMs =
                            15,

                        Bindings =
                        [
                            new()
                            {
                                ActionId =
                                    GamepadActionIds
                                        .MotionPrecision,

                                Control =
                                    GamepadControl.RightTrigger
                            },

                            new()
                            {
                                ActionId =
                                    GamepadActionIds
                                        .WeaponFire,

                                Control =
                                    GamepadControl.RightShoulder
                            }
                        ]
                    }
            };
        }

        public void Dispose()
        {
            if (Directory.Exists(
                    _temporaryDirectory))
            {
                Directory.Delete(
                    _temporaryDirectory,
                    recursive: true);
            }
        }
    }
}