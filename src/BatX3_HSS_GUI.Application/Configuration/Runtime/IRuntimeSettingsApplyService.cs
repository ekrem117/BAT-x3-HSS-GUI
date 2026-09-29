using BatX3_HSS_GUI.Application.Configuration.Input;

namespace BatX3_HSS_GUI.Application.Configuration.Runtime
{
    public interface IRuntimeSettingsApplyService
    {
        Task<RuntimeSettingsApplyResult> ApplyAsync(NetworkSettings networkSettings, InputSettings inputSettings, CancellationToken cancellationToken = default);
    }
}