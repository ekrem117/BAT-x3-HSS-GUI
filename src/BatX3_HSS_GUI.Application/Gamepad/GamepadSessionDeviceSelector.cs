namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadSessionDeviceSelector
    {
        private readonly GamepadDeviceSelectionState
            _baseSelectionState;

        private bool
            _configurationInitialized;

        private string?
            _lastConfiguredDeviceId;

        private string?
            _sessionDeviceId;

        public GamepadSessionDeviceSelector(
            GamepadDeviceSelectionState baseSelectionState)
        {
            ArgumentNullException.ThrowIfNull(
                baseSelectionState);

            _baseSelectionState =
                baseSelectionState;
        }

        public string? ResolveActiveDeviceId(
            GamepadSettings settings,
            IReadOnlyList<GamepadDeviceInfo> devices)
        {
            ArgumentNullException.ThrowIfNull(
                settings);

            ArgumentNullException.ThrowIfNull(
                devices);

            string? configuredDeviceId =
                NormalizeDeviceId(
                    settings.ActiveDeviceId);

            bool configurationChanged =
                !_configurationInitialized ||
                !string.Equals(
                    _lastConfiguredDeviceId,
                    configuredDeviceId,
                    StringComparison.Ordinal);

            if (configurationChanged)
            {
                _configurationInitialized =
                    true;

                _lastConfiguredDeviceId =
                    configuredDeviceId;

                /*
                 * Kullanıcı explicit cihaz değiştirdiyse session lock
                 * yeni seçime taşınır.
                 *
                 * Explicit seçim null'a dönerse yeni bir default seçim
                 * yapılmasına bir kez izin verilir.
                 */
                _sessionDeviceId =
                    configuredDeviceId;
            }

            if (configuredDeviceId is not null)
            {
                return configuredDeviceId;
            }

            /*
             * Session içinde bir gamepad seçilmişse cihaz kaybolsa bile
             * başka gamepad'e sessizce geçilmez.
             *
             * Aynı cihaz geri dönerse tekrar kullanılabilir.
             */
            if (_sessionDeviceId is not null)
            {
                return _sessionDeviceId;
            }

            string? resolvedDeviceId =
                _baseSelectionState
                    .ResolveActiveDeviceId(
                        settings,
                        devices);

            if (!string.IsNullOrWhiteSpace(
                    resolvedDeviceId))
            {
                _sessionDeviceId =
                    resolvedDeviceId;
            }

            return resolvedDeviceId;
        }

        private static string? NormalizeDeviceId(
            string? deviceId)
        {
            return string.IsNullOrWhiteSpace(
                    deviceId)
                ? null
                : deviceId;
        }
    }
}
