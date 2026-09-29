namespace BatX3_HSS_GUI.Application.Parameters.UI
{
    public sealed record ParameterUiDefinition
    {
        public required string ParameterName { get; init; }

        public required string DisplayName { get; init; }

        public required ParameterControlType ControlType { get; init; }

        public required string GroupName { get; init; }

        public int DisplayOrder { get; init; }

        public IReadOnlyList<ParameterOption> Options { get; init; } = Array.Empty<ParameterOption>();

        public ParameterUiPage Page { get; init; } = ParameterUiPage.Configuration;

        public string? Description { get; init; }

        public bool IsProminent { get; init; }
    }
}