namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadApplicationActivityState
    {
        private readonly object
            _syncRoot =
                new();

        private GamepadApplicationActivitySnapshot
            _snapshot =
                new(
                    0,
                    true);

        public GamepadApplicationActivitySnapshot
            GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _snapshot;
            }
        }

        public void SetActive(
            bool isActive)
        {
            lock (_syncRoot)
            {
                if (_snapshot.IsActive ==
                    isActive)
                {
                    return;
                }

                _snapshot =
                    new GamepadApplicationActivitySnapshot(
                        checked(
                            _snapshot.Version + 1),
                        isActive);
            }
        }
    }
}