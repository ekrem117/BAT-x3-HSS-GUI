namespace BatX3_HSS_GUI.Application.Input
{
    public interface IManualInputCoordinator
    {
        Task SetActiveAsync(ManualInputSource source, string actionId, bool isActive, CancellationToken cancellationToken = default);

        Task ReleaseSourceAsync(ManualInputSource source, CancellationToken cancellationToken = default);

        Task ReleaseAllAsync(CancellationToken cancellationToken = default);
    }
}