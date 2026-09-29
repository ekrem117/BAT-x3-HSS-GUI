namespace BatX3_HSS_GUI.Application.Gamepad
{
    public interface IGamepadDeviceProvider
    {
        IReadOnlyList<GamepadDeviceInfo> GetConnectedDevices();

        bool TryRead(string deviceId, out GamepadReadingSnapshot? snapshot);
    }
}