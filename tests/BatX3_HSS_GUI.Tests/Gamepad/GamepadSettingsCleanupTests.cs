using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadSettingsCleanupTests
    {
        [Fact]
        public void SupportedActions_ShouldContainOnlyCurrentBindableActions()
        {
            Assert.Equal(
                6,
                GamepadActionIds.Supported.Count);

            Assert.Contains(
                GamepadActionIds.MotionPrecision,
                GamepadActionIds.Supported);

            Assert.Contains(
                GamepadActionIds.WeaponFire,
                GamepadActionIds.Supported);

            Assert.Contains(
                GamepadActionIds.WeaponArmToggle,
                GamepadActionIds.Supported);

            Assert.Contains(
                GamepadActionIds.ModeManual,
                GamepadActionIds.Supported);

            Assert.Contains(
                GamepadActionIds.ModeIdle,
                GamepadActionIds.Supported);

            Assert.Contains(
                GamepadActionIds.ModeEmergencyStop,
                GamepadActionIds.Supported);
        }

        [Theory]
        [InlineData("Motion.Up")]
        [InlineData("Motion.Down")]
        [InlineData("Motion.Left")]
        [InlineData("Motion.Right")]
        public void LegacyDigitalMotionAction_ShouldNotBeSupported(
            string actionId)
        {
            Assert.DoesNotContain(
                actionId,
                GamepadActionIds.Supported);
        }

        [Fact]
        public void DefaultSettings_ShouldBeValid()
        {
            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    new GamepadSettings());

            Assert.Empty(
                errors);
        }

        [Fact]
        public void LegacyDigitalMotionBinding_ShouldBeRejected()
        {
            GamepadSettings settings =
                new();

            settings.Bindings.Add(
                new GamepadBindingSettings
                {
                    ActionId =
                        "Motion.Up",

                    Control =
                        GamepadControl.DPadUp
                });

            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    settings);

            Assert.Contains(
                errors,
                error =>
                    error.Contains(
                        "Desteklenmeyen gamepad action",
                        StringComparison.Ordinal));
        }
    }
}