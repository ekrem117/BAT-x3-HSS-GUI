namespace BatX3_HSS_GUI.Domain.Communication.Command
{
    public sealed record CommandParameterResult(CommandStatus Status, object? Value);
}