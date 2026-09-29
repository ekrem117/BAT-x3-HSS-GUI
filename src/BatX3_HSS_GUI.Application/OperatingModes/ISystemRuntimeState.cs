using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.OperatingModes
{
    public interface ISystemRuntimeState
    {
        SystemOperatingMode? CurrentMode { get; }

        void UpdateMode(SystemOperatingMode mode);

        void MarkUnknown();
    }
}