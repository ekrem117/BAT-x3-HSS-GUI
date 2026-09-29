using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadDeviceSelectionStateTests
    {
        [Fact]
        public void FirstDevice_ShouldBecomeAutomaticSelection()
        {
            GamepadDeviceSelectionState selection =
                new();

            GamepadSettings settings =
                new();

            GamepadDeviceInfo[] devices =
            [
                new(
                    "A",
                    "Pad A"),

                new(
                    "B",
                    "Pad B")
            ];

            string? active =
                selection.ResolveActiveDeviceId(
                    settings,
                    devices);

            Assert.Equal(
                "A",
                active);
        }

        [Fact]
        public void RemovedAutomaticDevice_ShouldNotFailOver()
        {
            GamepadDeviceSelectionState selection =
                new();

            GamepadSettings settings =
                new();

            selection.ResolveActiveDeviceId(
                settings,
                [
                    new(
                        "A",
                        "Pad A"),

                    new(
                        "B",
                        "Pad B")
                ]);

            string? active =
                selection.ResolveActiveDeviceId(
                    settings,
                    [
                        new(
                            "B",
                            "Pad B")
                    ]);

            Assert.Null(
                active);
        }

        [Fact]
        public void RemovedAutomaticDevice_WhenReconnected_ShouldBecomeActiveAgain()
        {
            GamepadDeviceSelectionState selection =
                new();

            GamepadSettings settings =
                new();

            selection.ResolveActiveDeviceId(
                settings,
                [
                    new(
                        "A",
                        "Pad A")
                ]);

            Assert.Null(
                selection.ResolveActiveDeviceId(
                    settings,
                    Array.Empty<GamepadDeviceInfo>()));

            string? active =
                selection.ResolveActiveDeviceId(
                    settings,
                    [
                        new(
                            "A",
                            "Pad A")
                    ]);

            Assert.Equal(
                "A",
                active);
        }

        [Fact]
        public void ExplicitDevice_ShouldOverrideAutomaticSelection()
        {
            GamepadDeviceSelectionState selection =
                new();

            GamepadSettings settings =
                new()
                {
                    ActiveDeviceId =
                        "B"
                };

            string? active =
                selection.ResolveActiveDeviceId(
                    settings,
                    [
                        new(
                            "A",
                            "Pad A"),

                        new(
                            "B",
                            "Pad B")
                    ]);

            Assert.Equal(
                "B",
                active);
        }
    }
}