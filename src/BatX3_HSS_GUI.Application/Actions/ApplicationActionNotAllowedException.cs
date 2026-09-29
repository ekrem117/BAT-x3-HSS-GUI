namespace BatX3_HSS_GUI.Application.Actions
{
    public sealed class ApplicationActionNotAllowedException : InvalidOperationException
    {
        public ApplicationActionNotAllowedException(string actionId, string message) : base(message)
        {
            ActionId = actionId;
        }

        public string ActionId { get; }
    }
}