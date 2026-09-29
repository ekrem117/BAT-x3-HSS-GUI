namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed record GamepadControlTransitions(
        IReadOnlySet<GamepadControl> Pressed,
        IReadOnlySet<GamepadControl> Released)
    {
        public static GamepadControlTransitions Empty
        {
            get;
        } =
            new(
                new HashSet<GamepadControl>(),
                new HashSet<GamepadControl>());
    }
}