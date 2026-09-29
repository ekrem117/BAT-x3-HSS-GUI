namespace BatX3_HSS_GUI.Domain.Communication.Command
{
    public enum CommandStatus
    {
        Success = 0,
        UndefinedMethodType = 1,
        UndefinedKey = 2,
        MessageFormatError = 3,
        ValueTypeError = 4,
        AccessTypeError = 5,
        InvalidValue = 6,
        ValueLengthError = 7,
        ValueRangeError = 8,
        InternalError = 9,
        ReadError = 10,
        WriteError = 11
    }
}