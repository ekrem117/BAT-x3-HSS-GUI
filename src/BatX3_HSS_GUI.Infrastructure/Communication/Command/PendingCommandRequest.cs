using BatX3_HSS_GUI.Domain.Communication.Command;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Command
{
    internal sealed class PendingCommandRequest
    {
        public PendingCommandRequest(CommandResponseMethod expectedMethod)
        {
            ExpectedMethod = expectedMethod;

            Completion = new TaskCompletionSource<CommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public CommandResponseMethod ExpectedMethod { get; }

        public TaskCompletionSource<CommandResponse> Completion { get; }
    }
}