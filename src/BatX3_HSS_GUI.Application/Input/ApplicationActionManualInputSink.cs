using BatX3_HSS_GUI.Application.Actions;

namespace BatX3_HSS_GUI.Application.Input
{
    public sealed class ApplicationActionManualInputSink : IManualInputActionSink
    {
        private readonly IApplicationActionService _applicationActionService;

        public ApplicationActionManualInputSink(IApplicationActionService applicationActionService)
        {
            _applicationActionService = applicationActionService;
        }

        public Task PressAsync(string actionId, CancellationToken cancellationToken = default)
        {
            return _applicationActionService.PressAsync(actionId, cancellationToken);
        }

        public Task ReleaseAsync(string actionId, CancellationToken cancellationToken = default)
        {
            return _applicationActionService.ReleaseAsync(actionId, cancellationToken);
        }
    }
}