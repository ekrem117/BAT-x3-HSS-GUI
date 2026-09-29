namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadRuntimeState
    {
        private readonly object _syncRoot =
            new();

        private GamepadRuntimeSnapshot _snapshot =
            new(
                GamepadRuntimeStatus.Disconnected,
                null,
                null,
                null);

        public GamepadRuntimeSnapshot GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _snapshot;
            }
        }

        public void Update(
            GamepadRuntimeStatus status,
            string? deviceId = null,
            string? deviceName = null,
            string? lastError = null)
        {
            lock (_syncRoot)
            {
                _snapshot =
                    new GamepadRuntimeSnapshot(
                        status,
                        deviceId,
                        deviceName,
                        lastError);
            }
        }
    }
}