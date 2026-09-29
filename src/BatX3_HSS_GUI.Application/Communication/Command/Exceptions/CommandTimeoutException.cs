namespace BatX3_HSS_GUI.Application.Communication.Command.Exceptions
{
    public sealed class CommandTimeoutException : TimeoutException
    {
        public CommandTimeoutException(int messageId, TimeSpan timeout)
            : base($"Command request timed out. " + $"MessageId={messageId}, Timeout={timeout.TotalMilliseconds:0} ms.")
        {
            MessageId = messageId;
            Timeout = timeout;
        }

        public int MessageId { get; }

        public TimeSpan Timeout { get; }
    }
}