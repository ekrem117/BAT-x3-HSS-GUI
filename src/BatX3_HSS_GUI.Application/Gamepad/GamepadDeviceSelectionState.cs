namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadDeviceSelectionState
    {
        private string? _autoSelectedDeviceId;

        public string? ResolveActiveDeviceId(
            GamepadSettings settings,
            IReadOnlyCollection<GamepadDeviceInfo> devices)
        {
            ArgumentNullException.ThrowIfNull(
                settings);

            ArgumentNullException.ThrowIfNull(
                devices);

            if (!settings.Enabled)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(
                    settings.ActiveDeviceId))
            {
                return devices.Any(
                    device =>
                        string.Equals(
                            device.Id,
                            settings.ActiveDeviceId,
                            StringComparison.Ordinal))
                    ? settings.ActiveDeviceId
                    : null;
            }

            if (_autoSelectedDeviceId is null)
            {
                GamepadDeviceInfo? firstDevice =
                    devices.FirstOrDefault();

                if (firstDevice is null)
                {
                    return null;
                }

                _autoSelectedDeviceId =
                    firstDevice.Id;
            }

            return devices.Any(
                device =>
                    string.Equals(
                        device.Id,
                        _autoSelectedDeviceId,
                        StringComparison.Ordinal))
                ? _autoSelectedDeviceId
                : null;
        }

        public void ResetAutomaticSelection()
        {
            _autoSelectedDeviceId =
                null;
        }
    }
}