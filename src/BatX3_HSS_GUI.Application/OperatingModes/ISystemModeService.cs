using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.OperatingModes
{
    public interface ISystemModeService
    {
        Task SetModeAsync(SystemOperatingMode targetMode, CancellationToken cancellationToken = default);
    }
}