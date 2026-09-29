using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Client.ViewModels.Settings
{
    public static class GamepadControlCatalog
    {
        private static readonly IReadOnlyList<
            GamepadControlOptionViewModel>
            Options =
            [
                new(
                    GamepadControl.A,
                    "A"),

                new(
                    GamepadControl.B,
                    "B"),

                new(
                    GamepadControl.X,
                    "X"),

                new(
                    GamepadControl.Y,
                    "Y"),

                new(
                    GamepadControl.LeftShoulder,
                    "Sol Omuz (L1)"),

                new(
                    GamepadControl.RightShoulder,
                    "Sağ Omuz (R1)"),

                new(
                    GamepadControl.LeftTrigger,
                    "Sol Tetik (L2)"),

                new(
                    GamepadControl.RightTrigger,
                    "Sağ Tetik (R2)"),

                new(
                    GamepadControl.View,
                    "View / Select"),

                new(
                    GamepadControl.Menu,
                    "Menu / Start"),

                new(
                    GamepadControl.DPadUp,
                    "D-Pad Yukarı"),

                new(
                    GamepadControl.DPadDown,
                    "D-Pad Aşağı"),

                new(
                    GamepadControl.DPadLeft,
                    "D-Pad Sol"),

                new(
                    GamepadControl.DPadRight,
                    "D-Pad Sağ"),

                new(
                    GamepadControl.LeftThumbstick,
                    "Sol Stick Basma"),

                new(
                    GamepadControl.RightThumbstick,
                    "Sağ Stick Basma")
            ];

        public static IReadOnlyList<
            GamepadControlOptionViewModel>
            GetAvailableControls()
        {
            return Options;
        }
    }
}