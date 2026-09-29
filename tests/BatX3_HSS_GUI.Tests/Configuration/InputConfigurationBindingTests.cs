using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Gamepad;
using Microsoft.Extensions.Configuration;

namespace BatX3_HSS_GUI.Tests.Configuration
{
    public sealed class InputConfigurationBindingTests
    {
        [Fact]
        public void AppSettingsBinding_ShouldNotDuplicateGamepadBindings()
        {
            Dictionary<string, string?> values =
                new()
                {
                    ["Input:Gamepad:Enabled"] =
                        "true",

                    ["Input:Gamepad:Bindings:0:ActionId"] =
                        GamepadActionIds.MotionPrecision,

                    ["Input:Gamepad:Bindings:0:Control"] =
                        nameof(GamepadControl.RightTrigger),

                    ["Input:Gamepad:Bindings:1:ActionId"] =
                        GamepadActionIds.WeaponFire,

                    ["Input:Gamepad:Bindings:1:Control"] =
                        nameof(GamepadControl.RightShoulder),

                    ["Input:Gamepad:Bindings:2:ActionId"] =
                        GamepadActionIds.WeaponArmToggle,

                    ["Input:Gamepad:Bindings:2:Control"] =
                        nameof(GamepadControl.LeftShoulder),

                    ["Input:Gamepad:Bindings:3:ActionId"] =
                        GamepadActionIds.ModeManual,

                    ["Input:Gamepad:Bindings:3:Control"] =
                        nameof(GamepadControl.Menu),

                    ["Input:Gamepad:Bindings:4:ActionId"] =
                        GamepadActionIds.ModeIdle,

                    ["Input:Gamepad:Bindings:4:Control"] =
                        nameof(GamepadControl.View),

                    ["Input:Gamepad:Bindings:5:ActionId"] =
                        GamepadActionIds.ModeEmergencyStop,

                    ["Input:Gamepad:Bindings:5:Control"] =
                        nameof(GamepadControl.B)
                };

            IConfiguration configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        values)
                    .Build();

            InputSettings settings =
                new();

            settings.Gamepad.Bindings.Clear();

            configuration
                .GetSection(
                    InputSettings.SectionName)
                .Bind(
                    settings);

            Assert.Equal(
                6,
                settings.Gamepad.Bindings.Count);

            Assert.Equal(
                6,
                settings.Gamepad.Bindings
                    .Select(
                        binding =>
                            binding.ActionId)
                    .Distinct(
                        StringComparer.Ordinal)
                    .Count());

            Assert.Equal(
                6,
                settings.Gamepad.Bindings
                    .Select(
                        binding =>
                            binding.Control)
                    .Distinct()
                    .Count());

            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    settings.Gamepad);

            Assert.Empty(
                errors);
        }
    }
}
