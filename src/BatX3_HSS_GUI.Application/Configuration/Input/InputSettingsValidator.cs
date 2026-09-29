using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Application.Configuration.Input
{
    public sealed class InputSettingsValidator
    {
        private readonly IApplicationActionCatalog
            _actionCatalog;

        public InputSettingsValidator(
            IApplicationActionCatalog actionCatalog)
        {
            _actionCatalog =
                actionCatalog;
        }

        public IReadOnlyList<string> Validate(
            InputSettings settings)
        {
            ArgumentNullException.ThrowIfNull(
                settings);

            List<string> errors =
                new();

            ValidateShortcuts(
                settings,
                errors);

            ValidateGamepad(
                settings,
                errors);

            return errors;
        }

        private void ValidateShortcuts(
            InputSettings settings,
            ICollection<string> errors)
        {
            if (settings.Shortcuts is null)
            {
                errors.Add(
                    "Shortcut ayarları null olamaz.");

                return;
            }

            HashSet<string> assignedActions =
                new(
                    StringComparer.Ordinal);

            HashSet<string> assignedGestures =
                new(
                    StringComparer.OrdinalIgnoreCase);

            foreach (
                ShortcutBindingSettings shortcut
                in settings.Shortcuts)
            {
                if (shortcut is null)
                {
                    errors.Add(
                        "Shortcut tanımı null olamaz.");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        shortcut.ActionId))
                {
                    errors.Add(
                        "Shortcut ActionId boş olamaz.");

                    continue;
                }

                if (!_actionCatalog.TryGet(
                        shortcut.ActionId,
                        out ApplicationActionDefinition? action) ||
                    action is null)
                {
                    errors.Add(
                        $"Bilinmeyen application action: " +
                        $"{shortcut.ActionId}");

                    continue;
                }

                if (!action.AllowKeyboardShortcut)
                {
                    errors.Add(
                        $"'{action.DisplayName}' keyboard shortcut " +
                        "desteklemiyor.");
                }

                if (string.IsNullOrWhiteSpace(
                        shortcut.Key))
                {
                    errors.Add(
                        $"'{action.DisplayName}' için shortcut tuşu " +
                        "boş olamaz.");

                    continue;
                }

                if (!assignedActions.Add(
                        shortcut.ActionId))
                {
                    errors.Add(
                        $"'{action.DisplayName}' için birden fazla " +
                        "shortcut tanımlanamaz.");
                }

                string gesture =
                    $"{shortcut.Modifiers}:{shortcut.Key}";

                if (!assignedGestures.Add(
                        gesture))
                {
                    errors.Add(
                        $"'{shortcut.Key}' shortcut kombinasyonu " +
                        "birden fazla aksiyona atanamaz.");
                }
            }
        }

        private static void ValidateGamepad(
            InputSettings settings,
            ICollection<string> errors)
        {
            if (settings.Gamepad is null)
            {
                errors.Add(
                    "Gamepad ayarları null olamaz.");

                return;
            }

            GamepadSettingsValidator validator =
                new();

            foreach (
                string error
                in validator.Validate(
                    settings.Gamepad))
            {
                errors.Add(
                    error);
            }
        }
    }
}