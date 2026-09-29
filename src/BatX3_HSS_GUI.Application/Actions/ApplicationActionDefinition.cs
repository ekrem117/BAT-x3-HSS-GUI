using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.Actions
{
    public sealed record ApplicationActionDefinition
    {
        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        public required string ParameterName { get; init; }

        public required object Value { get; init; }

        public object? ReleaseValue { get; init; }

        public ApplicationActionInteractionType InteractionType { get; init; } = ApplicationActionInteractionType.Trigger;

        public bool AllowKeyboardShortcut { get; init; }

        public IReadOnlyList<SystemOperatingMode> AllowedOperatingModes { get; init; } = Array.Empty<SystemOperatingMode>();
    }
}