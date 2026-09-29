namespace BatX3_HSS_GUI.Application.Parameters
{
    public sealed class ParameterValidationException : Exception
    {
        public ParameterValidationException(string parameterName, string message) : base(message)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}