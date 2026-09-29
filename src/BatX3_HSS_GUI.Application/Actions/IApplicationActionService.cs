namespace BatX3_HSS_GUI.Application.Actions
{

    public interface IApplicationActionService
    {
        Task ExecuteAsync(string actionId, CancellationToken cancellationToken = default);

        Task PressAsync(string actionId, CancellationToken cancellationToken = default);

        Task ReleaseAsync(string actionId, CancellationToken cancellationToken = default);

        Task ReleaseAllMomentaryAsync(CancellationToken cancellationToken = default);
    }
}