using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Configuration.Input;

namespace BatX3_HSS_GUI.Tests.Input
{
    public sealed class DefaultShortcutSettingsTests
    {
        [Fact]
        public void DefaultOperationShortcuts_ShouldBeValid()
        {
            InputSettings settings =
                new()
                {
                    Shortcuts =
                    [
                        new ShortcutBindingSettings
                        {
                            ActionId = ApplicationActionIds.WeaponFire,
                            Key = "F",
                            Modifiers = ShortcutModifiers.None
                        },

                        new ShortcutBindingSettings
                        {
                            ActionId = ApplicationActionIds.MotionUp,
                            Key = "W",
                            Modifiers = ShortcutModifiers.None
                        },

                        new ShortcutBindingSettings
                        {
                            ActionId = ApplicationActionIds.MotionDown,
                            Key = "S",
                            Modifiers = ShortcutModifiers.None
                        },

                        new ShortcutBindingSettings
                        {
                            ActionId = ApplicationActionIds.MotionLeft,
                            Key = "A",
                            Modifiers = ShortcutModifiers.None
                        },

                        new ShortcutBindingSettings
                        {
                            ActionId = ApplicationActionIds.MotionRight,
                            Key = "D",
                            Modifiers = ShortcutModifiers.None
                        }
                    ]
                };

            InputSettingsValidator validator = new(new StaticApplicationActionCatalog());

            IReadOnlyList<string> errors = validator.Validate(settings);

            Assert.Empty(errors);
        }

        [Fact]
        public void DefaultOperationShortcuts_ShouldUseUniqueKeys()
        {
            InputSettings settings =
                new()
                {
                    Shortcuts =
                    [
                        Create(ApplicationActionIds.WeaponFire, "F"),
                        Create(ApplicationActionIds.MotionUp, "W"),
                        Create(ApplicationActionIds.MotionDown, "S"),
                        Create(ApplicationActionIds.MotionLeft, "A"),
                        Create(ApplicationActionIds.MotionRight, "D")
                    ]
                };

            string[] duplicates =
                settings.Shortcuts
                    .GroupBy(shortcut => $"{shortcut.Modifiers}:{shortcut.Key}", StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToArray();

            Assert.Empty(duplicates);
        }

        private static ShortcutBindingSettings Create(string actionId, string key)
        {
            return new ShortcutBindingSettings
            {
                ActionId = actionId,
                Key = key,
                Modifiers = ShortcutModifiers.None
            };
        }
    }
}