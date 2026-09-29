namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadReadingSnapshot
    {
        public double LeftStickX { get; init; }

        public double LeftStickY { get; init; }

        public double LeftTrigger { get; init; }

        public double RightTrigger { get; init; }

        public IReadOnlySet<GamepadControl> PressedControls { get; init; } = new HashSet<GamepadControl>();
    }
}
