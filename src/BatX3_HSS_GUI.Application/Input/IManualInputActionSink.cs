namespace BatX3_HSS_GUI.Application.Input
{
    public interface IManualInputActionSink
    {
        Task PressAsync(string actionId, CancellationToken cancellationToken = default);

        Task ReleaseAsync(string actionId, CancellationToken cancellationToken = default);
    }
}