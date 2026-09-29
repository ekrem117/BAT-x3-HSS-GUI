using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadSettingsComparerTests
    {
        [Fact]
        public void IdenticalSettings_ShouldBeEquivalent()
        {
            GamepadSettings left =
                new();

            GamepadSettings right =
                new();

            Assert.True(
                GamepadSettingsComparer.AreEquivalent(
                    left,
                    right));
        }

        [Fact]
        public void DifferentDeadzone_ShouldNotBeEquivalent()
        {
            GamepadSettings left =
                new()
                {
                    Deadzone =
                        0.15
                };

            GamepadSettings right =
                new()
                {
                    Deadzone =
                        0.25
                };

            Assert.False(
                GamepadSettingsComparer.AreEquivalent(
                    left,
                    right));
        }

        [Fact]
        public void BindingOrder_ShouldNotAffectEquivalence()
        {
            GamepadSettings left =
                new();

            GamepadSettings right =
                new()
                {
                    Bindings =
                        left.Bindings
                            .AsEnumerable()
                            .Reverse()
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

            Assert.True(
                GamepadSettingsComparer.AreEquivalent(
                    left,
                    right));
        }

        [Fact]
        public void ChangedBindingControl_ShouldNotBeEquivalent()
        {
            GamepadSettings left =
                new();

            GamepadSettings right =
                new();

            GamepadBindingSettings fireBinding =
                right.Bindings.First(
                    binding =>
                        binding.ActionId ==
                        GamepadActionIds.WeaponFire);

            fireBinding.Control =
                GamepadControl.X;

            Assert.False(
                GamepadSettingsComparer.AreEquivalent(
                    left,
                    right));
        }

        [Fact]
        public void ActiveDeviceIdComparison_ShouldBeCaseSensitive()
        {
            GamepadSettings left =
                new()
                {
                    ActiveDeviceId =
                        "PAD-A"
                };

            GamepadSettings right =
                new()
                {
                    ActiveDeviceId =
                        "pad-a"
                };

            Assert.False(
                GamepadSettingsComparer.AreEquivalent(
                    left,
                    right));
        }
    }
}