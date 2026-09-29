using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Tests.OperatingModes
{
    public sealed class SystemRuntimeStateTests
    {
        [Fact]
        public void NewState_ShouldStartUnknown()
        {
            SystemRuntimeState state = new();

            Assert.Null(state.CurrentMode);
        }

        [Fact]
        public void UpdateMode_ShouldStoreCurrentMode()
        {
            SystemRuntimeState state = new();

            state.UpdateMode(SystemOperatingMode.Manual);

            Assert.Equal(SystemOperatingMode.Manual, state.CurrentMode);
        }

        [Fact]
        public void MarkUnknown_ShouldClearCurrentMode()
        {
            SystemRuntimeState state = new();

            state.UpdateMode(SystemOperatingMode.Manual);

            state.MarkUnknown();

            Assert.Null(state.CurrentMode);
        }
    }
}