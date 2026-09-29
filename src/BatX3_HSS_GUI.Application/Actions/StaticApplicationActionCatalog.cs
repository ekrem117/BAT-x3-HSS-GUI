using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.Actions
{
    public sealed class StaticApplicationActionCatalog : IApplicationActionCatalog
    {
        private readonly IReadOnlyDictionary<string, ApplicationActionDefinition> _actions;

        public StaticApplicationActionCatalog()
        {
            ApplicationActionDefinition[] actions =
            [
                new()
                {
                    Id = ApplicationActionIds.WeaponFire,
                    DisplayName = "Ateşle",
                    ParameterName = ParameterNames.Weapon.Fire,
                    Value = 1,
                    ReleaseValue = null,
                    InteractionType = ApplicationActionInteractionType.Trigger,
                    AllowKeyboardShortcut = true
                },

                new()
                {
                    Id = ApplicationActionIds.MotionUp,
                    DisplayName = "Yukarı Hareket",
                    ParameterName = ParameterNames.Motion.ManualUp,
                    Value = 1,
                    ReleaseValue = 0,
                    InteractionType = ApplicationActionInteractionType.Momentary,
                    AllowKeyboardShortcut = true,
                    AllowedOperatingModes = [SystemOperatingMode.Manual]
                },

                new()
                {
                    Id = ApplicationActionIds.MotionDown,
                    DisplayName = "Aşağı Hareket",
                    ParameterName = ParameterNames.Motion.ManualDown,
                    Value = 1,
                    ReleaseValue = 0,
                    InteractionType = ApplicationActionInteractionType.Momentary,
                    AllowKeyboardShortcut = true,
                    AllowedOperatingModes = [SystemOperatingMode.Manual]
                },

                new()
                {
                    Id = ApplicationActionIds.MotionLeft,
                    DisplayName = "Sola Hareket",
                    ParameterName = ParameterNames.Motion.ManualLeft,
                    Value = 1,
                    ReleaseValue = 0,
                    InteractionType = ApplicationActionInteractionType.Momentary,
                    AllowKeyboardShortcut = true,
                    AllowedOperatingModes = [SystemOperatingMode.Manual]
                },

                new()
                {
                    Id = ApplicationActionIds.MotionRight,
                    DisplayName = "Sağa Hareket",
                    ParameterName = ParameterNames.Motion.ManualRight,
                    Value = 1,
                    ReleaseValue = 0,
                    InteractionType = ApplicationActionInteractionType.Momentary,
                    AllowKeyboardShortcut = true,
                    AllowedOperatingModes = [SystemOperatingMode.Manual]
                }
            ];

            _actions = actions.ToDictionary(action => action.Id, StringComparer.Ordinal);
        }

        public IReadOnlyCollection<ApplicationActionDefinition> GetAll()
        {
            return _actions.Values.ToArray();
        }

        public ApplicationActionDefinition GetRequired(string actionId)
        {
            if (!_actions.TryGetValue(actionId, out ApplicationActionDefinition? definition))
            {
                throw new KeyNotFoundException($"Application action bulunamadı: {actionId}");
            }

            return definition;
        }

        public bool TryGet(string actionId, out ApplicationActionDefinition? definition)
        {
            return _actions.TryGetValue(actionId, out definition);
        }
    }
}