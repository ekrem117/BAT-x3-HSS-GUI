using BatX3_HSS_GUI.Application.Gamepad;
using Windows.Gaming.Input;

namespace BatX3_HSS_GUI.Client.Input.Gamepad
{
    internal interface IRawGamepadProfile
    {
        string DisplayName
        {
            get;
        }

        bool IsMatch(
            RawGameController controller);

        bool TryRead(
            RawGameController controller,
            out GamepadReadingSnapshot? snapshot);
    }
}