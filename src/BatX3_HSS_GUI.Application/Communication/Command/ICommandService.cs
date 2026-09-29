using BatX3_HSS_GUI.Domain.Communication.Command;

namespace BatX3_HSS_GUI.Application.Communication.Command
{
    public interface ICommandService
    {
        bool IsRunning { get; }

        int LocalPort { get; }

        Task<CommandResponse> GetAsync(IReadOnlyCollection<string> parameterNames, CancellationToken cancellationToken = default);

        Task<CommandResponse> SetAsync(IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    }
}