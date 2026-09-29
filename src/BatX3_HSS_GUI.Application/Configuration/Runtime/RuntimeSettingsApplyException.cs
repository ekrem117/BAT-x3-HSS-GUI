namespace BatX3_HSS_GUI.Application.Configuration.Runtime
{
    public sealed class RuntimeSettingsApplyException : Exception
    {
        public RuntimeSettingsApplyException(string message, Exception innerException, bool rollbackSucceeded) : base(message, innerException)
        {
            RollbackSucceeded = rollbackSucceeded;
        }

        public bool RollbackSucceeded { get; }
    }
}