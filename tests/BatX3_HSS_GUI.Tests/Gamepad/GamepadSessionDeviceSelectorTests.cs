using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class
            GamepadSessionDeviceSelectorTests
    {
        [Fact]
        public void AutoSelectedDeviceDisconnect_ShouldNotFailOver()
        {
            GamepadSettings settings =
                new()
                {
                    ActiveDeviceId =
                        null
                };

            GamepadSessionDeviceSelector selector =
                new(
                    new GamepadDeviceSelectionState());

            GamepadDeviceInfo deviceA =
                new(
                    "A",
                    "Pad A");

            GamepadDeviceInfo deviceB =
                new(
                    "B",
                    "Pad B");

            string? initial =
                selector.ResolveActiveDeviceId(
                    settings,
                    [
                        deviceA,
                        deviceB
                    ]);

            Assert.NotNull(
                initial);

            string otherDeviceId =
                string.Equals(
                    initial,
                    deviceA.Id,
                    StringComparison.Ordinal)
                    ? deviceB.Id
                    : deviceA.Id;

            GamepadDeviceInfo otherDevice =
                string.Equals(
                    otherDeviceId,
                    deviceA.Id,
                    StringComparison.Ordinal)
                    ? deviceA
                    : deviceB;

            string? afterDisconnect =
                selector.ResolveActiveDeviceId(
                    settings,
                    [
                        otherDevice
                    ]);

            Assert.Equal(
                initial,
                afterDisconnect);
        }

        [Fact]
        public void ExplicitDeviceSelection_ShouldRemainSticky()
        {
            GamepadSettings settings =
                new()
                {
                    ActiveDeviceId =
                        "A"
                };

            GamepadSessionDeviceSelector selector =
                new(
                    new GamepadDeviceSelectionState());

            string? selected =
                selector.ResolveActiveDeviceId(
                    settings,
                    [
                        new GamepadDeviceInfo(
                            "B",
                            "Pad B")
                    ]);

            Assert.Equal(
                "A",
                selected);
        }

        [Fact]
        public void UserDeviceChange_ShouldReplaceSessionLock()
        {
            GamepadSettings settings =
                new()
                {
                    ActiveDeviceId =
                        "A"
                };

            GamepadSessionDeviceSelector selector =
                new(
                    new GamepadDeviceSelectionState());

            Assert.Equal(
                "A",
                selector.ResolveActiveDeviceId(
                    settings,
                    []));

            settings.ActiveDeviceId =
                "B";

            Assert.Equal(
                "B",
                selector.ResolveActiveDeviceId(
                    settings,
                    []));
        }
    }
}