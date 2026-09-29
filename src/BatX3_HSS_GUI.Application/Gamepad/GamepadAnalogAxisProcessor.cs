namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadAnalogAxisProcessor
    {
        private bool _neutralRequired =
            true;

        public bool NeutralRequired =>
            _neutralRequired;

        public void Reset()
        {
            _neutralRequired =
                true;
        }

        public GamepadAnalogState Process(
            GamepadReadingSnapshot snapshot,
            GamepadSettings settings)
        {
            ArgumentNullException.ThrowIfNull(
                snapshot);

            ArgumentNullException.ThrowIfNull(
                settings);

            double pan =
                ApplyDeadzoneAndRescale(
                    snapshot.LeftStickX,
                    settings.Deadzone);

            double tilt =
                ApplyDeadzoneAndRescale(
                    snapshot.LeftStickY,
                    settings.Deadzone);

            if (settings.InvertY)
            {
                tilt =
                    -tilt;
            }

            pan =
                NormalizeZero(
                    pan);

            tilt =
                NormalizeZero(
                    tilt);

            if (_neutralRequired)
            {
                if (!IsNeutral(
                        pan,
                        tilt))
                {
                    return GamepadAnalogState.NeutralRequired;
                }

                _neutralRequired =
                    false;

                return GamepadAnalogState.Neutral;
            }

            return new GamepadAnalogState(
                true,
                pan,
                tilt);
        }

        internal static double ApplyDeadzoneAndRescale(
            double value,
            double deadzone)
        {
            double clampedValue =
                Math.Clamp(
                    value,
                    -1.0,
                    1.0);

            double clampedDeadzone =
                Math.Clamp(
                    deadzone,
                    0.0,
                    0.999999);

            double magnitude =
                Math.Abs(
                    clampedValue);

            if (magnitude <=
                clampedDeadzone)
            {
                return 0.0;
            }

            double scaledMagnitude =
                (magnitude - clampedDeadzone) /
                (1.0 - clampedDeadzone);

            double result =
                Math.CopySign(
                    scaledMagnitude,
                    clampedValue);

            return Math.Clamp(
                result,
                -1.0,
                1.0);
        }

        private static bool IsNeutral(
            double pan,
            double tilt)
        {
            return
                pan == 0.0 &&
                tilt == 0.0;
        }

        private static double NormalizeZero(
            double value)
        {
            return value == 0.0
                ? 0.0
                : value;
        }
    }
}
