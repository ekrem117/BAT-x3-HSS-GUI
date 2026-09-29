using BatX3_HSS_GUI.Domain.Communication.Command;

namespace BatX3_HSS_GUI.Application.Parameters
{
    public sealed record ParameterOperationResult(string Name, CommandStatus Status, object? Value)
    {
        public bool IsSuccess => Status == CommandStatus.Success;
    }
}