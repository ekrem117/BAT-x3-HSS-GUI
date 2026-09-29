using BatX3_HSS_GUI.Application.Gamepad;
using Windows.Gaming.Input;

namespace BatX3_HSS_GUI.Client.Input.Gamepad
{
    internal sealed class Gp307CompatibleRawGamepadProfile :
        IRawGamepadProfile
    {
        private const int MinimumAxisCount =
            4;

        private const int MinimumButtonCount =
            12;

        private const int MinimumSwitchCount =
            1;

        public static Gp307CompatibleRawGamepadProfile Instance
        {
            get;
        } =
            new();

        private Gp307CompatibleRawGamepadProfile()
        {
        }

        public string DisplayName =>
            "GP-307 Uyumluluk Modu";

        public bool IsMatch(
            RawGameController controller)
        {
            ArgumentNullException.ThrowIfNull(
                controller);

            /*
             * Doğrulanmış cihaz profilleri bu fallback profile'dan
             * önce değerlendirilmelidir.
             *
             * Generic compatibility yalnız GP-307 mapping'ini
             * taşıyabilecek minimum fiziksel yapıya sahip cihazlarda
             * devreye girer.
             */
            return
                controller.AxisCount >=
                    MinimumAxisCount &&
                controller.ButtonCount >=
                    MinimumButtonCount &&
                controller.SwitchCount >=
                    MinimumSwitchCount;
        }

        public bool TryRead(
            RawGameController controller,
            out GamepadReadingSnapshot? snapshot)
        {
            ArgumentNullException.ThrowIfNull(
                controller);

            if (!IsMatch(
                    controller))
            {
                snapshot =
                    null;

                return false;
            }

            bool[] buttons =
                new bool[
                    controller.ButtonCount];

            GameControllerSwitchPosition[] switches =
                new GameControllerSwitchPosition[
                    controller.SwitchCount];

            double[] axes =
                new double[
                    controller.AxisCount];

            controller.GetCurrentReading(
                buttons,
                switches,
                axes);

            /*
             * Generic cihaz, fiziksel olarak doğrulanmış GP-307
             * mapping semantiğini kullanır:
             *
             * A0 = Left X
             * A1 = Left Y
             * A2 = Right X (V1 unused)
             * A3 = Right Y (V1 unused)
             *
             * S0 = D-Pad
             *
             * B0..B3   = A/B/X/Y
             * B4/B5    = L1/R1
             * B6/B7    = L2/R2
             * B8/B9    = View/Menu
             * B10/B11  = L3/R3
             */
            return Gp307RawGamepadProfile.TryMapReading(
                buttons,
                switches,
                axes,
                out snapshot);
        }
    }
}