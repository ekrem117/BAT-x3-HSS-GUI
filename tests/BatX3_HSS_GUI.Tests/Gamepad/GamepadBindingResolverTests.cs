using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadBindingResolverTests
    {
        [Fact]
        public void DefaultRightShoulder_ShouldResolveWeaponFire()
        {
            GamepadSettings settings =
                new();

            GamepadBindingResolver resolver =
                new();

            bool resolved =
                resolver.TryResolve(
                    settings,
                    GamepadControl.RightShoulder,
                    out string? actionId);

            Assert.True(
                resolved);

            Assert.Equal(
                GamepadActionIds.WeaponFire,
                actionId);
        }

        [Fact]
        public void DefaultLeftShoulder_ShouldResolveArmToggle()
        {
            GamepadSettings settings =
                new();

            GamepadBindingResolver resolver =
                new();

            bool resolved =
                resolver.TryResolve(
                    settings,
                    GamepadControl.LeftShoulder,
                    out string? actionId);

            Assert.True(
                resolved);

            Assert.Equal(
                GamepadActionIds.WeaponArmToggle,
                actionId);
        }

        [Fact]
        public void UnboundControl_ShouldNotResolve()
        {
            GamepadSettings settings =
                new();

            GamepadBindingResolver resolver =
                new();

            bool resolved =
                resolver.TryResolve(
                    settings,
                    GamepadControl.X,
                    out string? actionId);

            Assert.False(
                resolved);

            Assert.Null(
                actionId);
        }

        [Fact]
        public void ReplacementSettings_ShouldBeImmediatelyResolvable()
        {
            GamepadSettings settings =
                new();

            GamepadBindingSettings fireBinding =
                settings.Bindings.First(
                    binding =>
                        binding.ActionId ==
                        GamepadActionIds.WeaponFire);

            fireBinding.Control =
                GamepadControl.X;

            GamepadBindingResolver resolver =
                new();

            bool resolved =
                resolver.TryResolve(
                    settings,
                    GamepadControl.X,
                    out string? actionId);

            Assert.True(
                resolved);

            Assert.Equal(
                GamepadActionIds.WeaponFire,
                actionId);
        }
    }
}