namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed record GamepadControlCaptureSnapshot(long Version, bool IsActive, string? RequestedDeviceId);
}
