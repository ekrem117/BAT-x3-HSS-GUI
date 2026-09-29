namespace BatX3_HSS_GUI.Domain.Communication.Command
{
    public sealed class CommandResponse
    {
        public required CommandResponseMethod Method { get; init; }

        public int? MessageId { get; init; }

        public IReadOnlyDictionary<string, CommandParameterResult> Parameters
        { get; init; } = new Dictionary<string, CommandParameterResult>();

        public CommandStatus? ErrorStatus { get; init; }

        public bool IsError =>
            Method == CommandResponseMethod.ErrorResponse;
    }
}