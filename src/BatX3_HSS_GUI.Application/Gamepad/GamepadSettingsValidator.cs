namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadSettingsValidator
    {
        public IReadOnlyList<string> Validate(
            GamepadSettings settings)
        {
            ArgumentNullException.ThrowIfNull(
                settings);

            List<string> errors =
                new();

            if (settings.Deadzone < 0.0 ||
                settings.Deadzone >= 1.0)
            {
                errors.Add(
                    "Gamepad ölü bölge değeri " +
                    "0.0 ile 1.0 arasında olmalıdır.");
            }

            if (settings.TriggerThreshold <= 0.0 ||
                settings.TriggerThreshold > 1.0)
            {
                errors.Add(
                    "Gamepad trigger eşiği 0.0'dan büyük " +
                    "ve 1.0'a eşit veya küçük olmalıdır.");
            }

            if (settings.TriggerDebounceSamples < 1 ||
                settings.TriggerDebounceSamples > 20)
            {
                errors.Add(
                    "Gamepad trigger debounce örnek sayısı " +
                    "1 ile 20 arasında olmalıdır.");
            }

            if (settings.PollingIntervalMs < 5 ||
                settings.PollingIntervalMs > 100)
            {
                errors.Add(
                    "Gamepad polling süresi " +
                    "5 ile 100 ms arasında olmalıdır.");
            }

            if (settings.FireMinimumIntervalMs < 0 ||
                settings.FireMinimumIntervalMs > 10000)
            {
                errors.Add(
                    "Gamepad ateş minimum aralığı " +
                    "0 ile 10000 ms arasında olmalıdır.");
            }

            ValidateBindings(
                settings,
                errors);

            return errors;
        }

        private static void ValidateBindings(
            GamepadSettings settings,
            ICollection<string> errors)
        {
            if (settings.Bindings is null)
            {
                errors.Add(
                    "Gamepad binding listesi null olamaz.");

                return;
            }

            HashSet<GamepadControl> controls =
                new();

            HashSet<string> actions =
                new(
                    StringComparer.Ordinal);

            foreach (
                GamepadBindingSettings? binding
                in settings.Bindings)
            {
                if (binding is null)
                {
                    errors.Add(
                        "Gamepad binding tanımı null olamaz.");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        binding.ActionId))
                {
                    errors.Add(
                        "Gamepad action kimliği boş olamaz.");

                    continue;
                }

                if (!GamepadActionIds.Supported.Contains(
                        binding.ActionId))
                {
                    errors.Add(
                        $"Desteklenmeyen gamepad action kimliği: " +
                        $"{binding.ActionId}");

                    continue;
                }

                if (binding.Control ==
                    GamepadControl.None)
                {
                    errors.Add(
                        $"'{binding.ActionId}' için geçerli bir " +
                        "gamepad kontrolü seçilmelidir.");

                    continue;
                }

                if (!controls.Add(
                        binding.Control))
                {
                    errors.Add(
                        "Gamepad kontrolü birden fazla action'a " +
                        $"atanmış: {binding.Control}");
                }

                if (!actions.Add(
                        binding.ActionId))
                {
                    errors.Add(
                        "Gamepad action'ı birden fazla kontrole " +
                        $"atanmış: {binding.ActionId}");
                }
            }
        }
    }
}