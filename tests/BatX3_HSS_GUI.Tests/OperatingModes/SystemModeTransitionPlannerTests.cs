using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Tests.OperatingModes
{
    public sealed class SystemModeTransitionPlannerTests
    {
        [Fact]
        public void SameMode_ShouldProduceEmptyPath()
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.Manual, SystemOperatingMode.Manual);

            Assert.Empty(path);
        }

        [Fact]
        public void IdleToManual_ShouldBeDirect()
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.Idle, SystemOperatingMode.Manual);

            Assert.Equal([SystemOperatingMode.Manual], path);
        }

        [Fact]
        public void ManualToIdle_ShouldBeDirect()
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.Manual, SystemOperatingMode.Idle);

            Assert.Equal([SystemOperatingMode.Idle], path);
        }

        [Fact]
        public void ManualToAuto_ShouldPassThroughIdle()
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.Manual, SystemOperatingMode.Auto);

            Assert.Equal([SystemOperatingMode.Idle, SystemOperatingMode.Auto], path);
        }

        [Fact]
        public void AutoToLoopDemo_ShouldPassThroughIdle()
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.Auto, SystemOperatingMode.LoopDemo);

            Assert.Equal([SystemOperatingMode.Idle, SystemOperatingMode.LoopDemo], path);
        }

        [Theory]
        [InlineData(SystemOperatingMode.Idle)]
        [InlineData(SystemOperatingMode.Manual)]
        [InlineData(SystemOperatingMode.Auto)]
        [InlineData(SystemOperatingMode.LoopDemo)]
        public void AnyNormalModeToEmergencyStop_ShouldBeDirect(SystemOperatingMode currentMode)
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(currentMode, SystemOperatingMode.EmergencyStop);

            Assert.Equal([SystemOperatingMode.EmergencyStop], path);
        }

        [Fact]
        public void EmergencyStopToIdle_ShouldBeDirect()
        {
            IReadOnlyList<SystemOperatingMode> path = SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.EmergencyStop, SystemOperatingMode.Idle);

            Assert.Equal([SystemOperatingMode.Idle], path);
        }

        [Fact]
        public void EmergencyStopToAuto_ShouldBeRejected()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () =>
                        SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.EmergencyStop, SystemOperatingMode.Auto));

            Assert.Contains("yalnızca Bekleme moduna", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void EmergencyStopToIdle_ShouldReturnIdleOnly()
        {
            IReadOnlyList<SystemOperatingMode> path =
                SystemModeTransitionPlanner.CreateTransitionPath(SystemOperatingMode.EmergencyStop, SystemOperatingMode.Idle);

            Assert.Equal([SystemOperatingMode.Idle], path);
        }

        [Fact]
        public void EmergencyStop_To_Manual_ShouldBeRejected()
        {
            Assert.Throws<InvalidOperationException>(
                () =>
                    SystemModeTransitionPlanner.CreateTransitionPath(
                        SystemOperatingMode.EmergencyStop,
                        SystemOperatingMode.Manual));
        }

        [Fact]
        public void EmergencyStop_To_Idle_ShouldReturnIdleOnly()
        {
            IReadOnlyList<SystemOperatingMode> path =
                SystemModeTransitionPlanner.CreateTransitionPath(
                    SystemOperatingMode.EmergencyStop,
                    SystemOperatingMode.Idle);

            Assert.Equal(
                new[]
                {
            SystemOperatingMode.Idle
                },
                path);
        }

        [Fact]
        public void Manual_To_Auto_ShouldTransitionThroughIdle()
        {
            IReadOnlyList<SystemOperatingMode> path =
                SystemModeTransitionPlanner.CreateTransitionPath(
                    SystemOperatingMode.Manual,
                    SystemOperatingMode.Auto);

            Assert.Equal(
                new[]
                {
            SystemOperatingMode.Idle,
            SystemOperatingMode.Auto
                },
                path);
        }
    }
}