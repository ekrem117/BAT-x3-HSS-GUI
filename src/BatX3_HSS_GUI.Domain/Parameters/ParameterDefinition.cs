namespace BatX3_HSS_GUI.Domain.Parameters
{
    public sealed record ParameterDefinition
    {
        public required string Name { get; init; }

        public required ParameterValueType ValueType { get; init; }

        public required ParameterAccessType AccessType { get; init; }

        public double? Minimum { get; init; }

        public double? Maximum { get; init; }

        public string? Unit { get; init; }

        public bool IsTrigger { get; init; }

        public bool CanRead => AccessType is ParameterAccessType.ReadOnly or ParameterAccessType.ReadWrite;

        public bool CanWrite => AccessType is ParameterAccessType.ReadWrite or ParameterAccessType.WriteOnly;
    }
}