using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.OperatingModes
{
    public sealed class SystemRuntimeState :
        ISystemRuntimeState
    {
        private readonly object _syncRoot = new();

        private SystemOperatingMode? _currentMode;

        public SystemOperatingMode? CurrentMode
        {
            get
            {
                lock (_syncRoot)
                {
                    return _currentMode;
                }
            }
        }

        public void UpdateMode(SystemOperatingMode mode)
        {
            lock (_syncRoot)
            {
                _currentMode = mode;
            }
        }

        public void MarkUnknown()
        {
            lock (_syncRoot)
            {
                _currentMode = null;
            }
        }
    }
}