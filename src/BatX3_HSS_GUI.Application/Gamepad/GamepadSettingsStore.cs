namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadSettingsStore :
            IGamepadSettingsStore
    {
        private readonly object
            _syncRoot =
                new();

        private GamepadSettingsSnapshot
            _current;

        private long
            _version;

        public GamepadSettingsStore(
            GamepadSettings initialSettings)
        {
            ArgumentNullException.ThrowIfNull(
                initialSettings);

            Validate(
                initialSettings);

            _version =
                1;

            _current =
                new GamepadSettingsSnapshot(
                    _version,
                    Clone(
                        initialSettings));
        }

        public GamepadSettingsSnapshot GetSnapshot()
        {
            return Volatile.Read(
                ref _current);
        }

        public void Replace(
            GamepadSettings settings)
        {
            ArgumentNullException.ThrowIfNull(
                settings);

            Validate(
                settings);

            GamepadSettings replacement =
                Clone(
                    settings);

            lock (_syncRoot)
            {
                GamepadSettingsSnapshot current =
                    _current;

                if (GamepadSettingsComparer.AreEquivalent(
                        current.Settings,
                        replacement))
                {
                    return;
                }

                checked
                {
                    _version++;
                }

                GamepadSettingsSnapshot snapshot =
                    new(
                        _version,
                        replacement);

                Volatile.Write(
                    ref _current,
                    snapshot);
            }
        }

        private static void Validate(
            GamepadSettings settings)
        {
            GamepadSettingsValidator validator =
                new();

            IReadOnlyList<string> errors =
                validator.Validate(
                    settings);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        errors));
            }
        }

        private static GamepadSettings Clone(
            GamepadSettings source)
        {
            return new GamepadSettings
            {
                Enabled =
                    source.Enabled,

                ActiveDeviceId =
                    source.ActiveDeviceId,

                InvertY =
                    source.InvertY,

                Deadzone =
                    source.Deadzone,

                TriggerThreshold =
                    source.TriggerThreshold,

                TriggerDebounceSamples =
                    source.TriggerDebounceSamples,

                PollingIntervalMs =
                    source.PollingIntervalMs,

                FireMinimumIntervalMs =
                    source.FireMinimumIntervalMs,

                Bindings =
                    source.Bindings
                        .Select(
                            binding =>
                                new GamepadBindingSettings
                                {
                                    ActionId =
                                        binding.ActionId,

                                    Control =
                                        binding.Control
                                })
                        .ToList()
            };
        }
    }
}