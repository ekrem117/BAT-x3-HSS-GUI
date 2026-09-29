namespace BatX3_HSS_GUI.Application.Gamepad
{
    public interface IGamepadSettingsStore
    {
        GamepadSettingsSnapshot GetSnapshot();

        void Replace(GamepadSettings settings);
    }
}