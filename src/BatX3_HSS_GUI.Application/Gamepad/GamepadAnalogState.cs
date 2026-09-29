namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed record GamepadAnalogState(
           bool IsReady,
           double Pan,
           double Tilt)
    {
        public static GamepadAnalogState NeutralRequired
        {
            get;
        } =
            new(
                false,
                0.0,
                0.0);

        public static GamepadAnalogState Neutral
        {
            get;
        } =
            new(
                true,
                0.0,
                0.0);
    }
}