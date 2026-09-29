namespace BatX3_HSS_GUI.Application.Communication.Command.Exceptions
{
    public sealed class CommandProtocolException : Exception
    {
        public CommandProtocolException(string message) : base(message) { }

        public CommandProtocolException(string message, Exception innerException) : base(message, innerException) { }
    }
}