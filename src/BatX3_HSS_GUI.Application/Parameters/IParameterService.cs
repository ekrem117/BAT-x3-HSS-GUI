namespace BatX3_HSS_GUI.Application.Parameters
{
    public interface IParameterService
    {
        Task<ParameterOperationResult> GetAsync(string parameterName, CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, ParameterOperationResult>> GetAsync(IReadOnlyCollection<string> parameterNames, CancellationToken cancellationToken = default);

        Task<ParameterOperationResult> SetAsync(string parameterName, object value, CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, ParameterOperationResult>> SetAsync(IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken = default);
    }
}