namespace BatX3_HSS_GUI.Application.Configuration.Runtime
{
    public sealed record RuntimeSettingsApplyResult
    {
        public required bool NetworkReconfigured { get; init; }
    }
}