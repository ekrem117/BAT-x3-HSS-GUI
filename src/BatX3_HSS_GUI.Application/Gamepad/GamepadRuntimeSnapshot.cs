namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed record GamepadRuntimeSnapshot(
           GamepadRuntimeStatus Status,
           string? DeviceId,
           string? DeviceName,
           string? LastError);
}