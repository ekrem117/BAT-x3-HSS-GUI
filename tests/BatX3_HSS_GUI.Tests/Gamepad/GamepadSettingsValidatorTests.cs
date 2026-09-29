using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadSettingsValidatorTests
    {
        [Fact]
        public void DefaultSettings_ShouldBeValid()
        {
            GamepadSettings settings =
                new();

            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    settings);

            Assert.Empty(
                errors);
        }
        [Fact]
        public void DuplicateControl_ShouldBeRejected()
        {
            GamepadSettings settings =
                new();

            GamepadBindingSettings idleBinding =
                settings.Bindings.Single(
                    binding =>
                        binding.ActionId ==
                        GamepadActionIds.ModeIdle);

            idleBinding.Control =
                GamepadControl.RightShoulder;

            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    settings);

            Assert.Contains(
                errors,
                error =>
                    error.Contains(
                        "birden fazla action",
                        StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void InvalidTriggerThreshold_ShouldBeRejected(
     double triggerThreshold)
        {
            GamepadSettings settings =
                new()
                {
                    TriggerThreshold =
                        triggerThreshold
                };

            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    settings);

            Assert.Contains(
                errors,
                error =>
                    error.Contains(
                        "trigger eşiği",
                        StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void UnsupportedAction_ShouldBeRejected()
        {
            GamepadSettings settings =
                new();

            settings.Bindings.Clear();

            settings.Bindings.Add(
                new GamepadBindingSettings
                {
                    ActionId =
                        "Unknown.Action",

                    Control =
                        GamepadControl.A
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
                        "Desteklenmeyen",
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}