using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Tests.Actions
{
    public sealed class ApplicationActionServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_TriggerAction_ShouldSendTriggerValue()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService);

            await service.ExecuteAsync(
                ApplicationActionIds.WeaponFire);

            ParameterSetCall call =
                Assert.Single(
                    parameterService.SetCalls);

            Assert.Equal(
                ParameterNames.Weapon.Fire,
                call.ParameterName);

            Assert.Equal(
                1,
                call.Value);
        }

        [Fact]
        public async Task ExecuteAsync_MomentaryAction_ShouldBeRejected()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService,
                    SystemOperatingMode.Manual);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await service.ExecuteAsync(
                        ApplicationActionIds.MotionUp);
                });

            Assert.Empty(
                parameterService.SetCalls);
        }

        [Fact]
        public async Task PressAsync_MotionInManualMode_ShouldSendPressValue()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService,
                    SystemOperatingMode.Manual);

            await service.PressAsync(
                ApplicationActionIds.MotionUp);

            ParameterSetCall call =
                Assert.Single(
                    parameterService.SetCalls);

            Assert.Equal(
                ParameterNames.Motion.ManualUp,
                call.ParameterName);

            Assert.Equal(
                1,
                call.Value);
        }

        [Theory]
        [InlineData(SystemOperatingMode.Idle)]
        [InlineData(SystemOperatingMode.Auto)]
        [InlineData(SystemOperatingMode.LoopDemo)]
        [InlineData(SystemOperatingMode.EmergencyStop)]
        public async Task PressAsync_MotionOutsideManualMode_ShouldBeRejected(
            SystemOperatingMode mode)
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService,
                    mode);

            await Assert.ThrowsAsync<ApplicationActionNotAllowedException>(
                async () =>
                {
                    await service.PressAsync(
                        ApplicationActionIds.MotionUp);
                });

            Assert.Empty(
                parameterService.SetCalls);
        }

        [Fact]
        public async Task PressAsync_MotionWithUnknownMode_ShouldBeRejected()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService,
                    currentMode: null);

            await Assert.ThrowsAsync<ApplicationActionNotAllowedException>(
                async () =>
                {
                    await service.PressAsync(
                        ApplicationActionIds.MotionUp);
                });

            Assert.Empty(
                parameterService.SetCalls);
        }

        [Fact]
        public async Task ReleaseAsync_MomentaryAction_ShouldSendReleaseValue()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService,
                    SystemOperatingMode.Manual);

            await service.PressAsync(
                ApplicationActionIds.MotionUp);

            parameterService.SetCalls.Clear();

            await service.ReleaseAsync(
                ApplicationActionIds.MotionUp);

            ParameterSetCall call =
                Assert.Single(
                    parameterService.SetCalls);

            Assert.Equal(
                ParameterNames.Motion.ManualUp,
                call.ParameterName);

            Assert.Equal(
                0,
                call.Value);
        }

        [Fact]
        public async Task ReleaseAsync_ShouldRemainAllowedAfterLeavingManualMode()
        {
            RecordingParameterService parameterService =
                new();

            SystemRuntimeState runtimeState =
                new();

            runtimeState.UpdateMode(
                SystemOperatingMode.Manual);

            ApplicationActionService service =
                new(
                    new StaticApplicationActionCatalog(),
                    parameterService,
                    runtimeState);

            await service.PressAsync(
                ApplicationActionIds.MotionLeft);

            runtimeState.UpdateMode(
                SystemOperatingMode.Auto);

            parameterService.SetCalls.Clear();

            await service.ReleaseAsync(
                ApplicationActionIds.MotionLeft);

            ParameterSetCall call =
                Assert.Single(
                    parameterService.SetCalls);

            Assert.Equal(
                ParameterNames.Motion.ManualLeft,
                call.ParameterName);

            Assert.Equal(
                0,
                call.Value);
        }

        [Fact]
        public async Task ReleaseAsync_TriggerAction_ShouldNotSendAnything()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService);

            await service.ReleaseAsync(
                ApplicationActionIds.WeaponFire);

            Assert.Empty(
                parameterService.SetCalls);
        }

        [Fact]
        public async Task ReleaseAllMomentaryAsync_ShouldReleaseAllActiveMotionActions()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService,
                    SystemOperatingMode.Manual);

            await service.PressAsync(
                ApplicationActionIds.MotionUp);

            await service.PressAsync(
                ApplicationActionIds.MotionRight);

            parameterService.SetCalls.Clear();

            await service.ReleaseAllMomentaryAsync();

            Assert.Equal(
                2,
                parameterService.SetCalls.Count);

            Assert.Contains(
                parameterService.SetCalls,
                call =>
                    call.ParameterName ==
                        ParameterNames.Motion.ManualUp &&
                    Equals(
                        call.Value,
                        0));

            Assert.Contains(
                parameterService.SetCalls,
                call =>
                    call.ParameterName ==
                        ParameterNames.Motion.ManualRight &&
                    Equals(
                        call.Value,
                        0));
        }

        [Fact]
        public async Task PressAsync_TriggerAction_ShouldSendTriggerValue()
        {
            RecordingParameterService parameterService =
                new();

            ApplicationActionService service =
                CreateService(
                    parameterService);

            await service.PressAsync(
                ApplicationActionIds.WeaponFire);

            ParameterSetCall call =
                Assert.Single(
                    parameterService.SetCalls);

            Assert.Equal(
                ParameterNames.Weapon.Fire,
                call.ParameterName);

            Assert.Equal(
                1,
                call.Value);
        }

        private static ApplicationActionService CreateService(
            RecordingParameterService parameterService,
            SystemOperatingMode? currentMode = null)
        {
            SystemRuntimeState runtimeState =
                new();

            if (currentMode is not null)
            {
                runtimeState.UpdateMode(
                    currentMode.Value);
            }

            return new ApplicationActionService(
                new StaticApplicationActionCatalog(),
                parameterService,
                runtimeState);
        }

        private sealed class RecordingParameterService :
            IParameterService
        {
            public List<ParameterSetCall> SetCalls { get; } =
                [];

            public Task<ParameterOperationResult> GetAsync(
                string parameterName,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<
                IReadOnlyDictionary<string, ParameterOperationResult>>
                GetAsync(
                    IReadOnlyCollection<string> parameterNames,
                    CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ParameterOperationResult> SetAsync(
                string parameterName,
                object value,
                CancellationToken cancellationToken = default)
            {
                SetCalls.Add(
                    new ParameterSetCall(
                        parameterName,
                        value));

                return Task.FromResult<ParameterOperationResult>(
                    null!);
            }

            public Task<
                IReadOnlyDictionary<string, ParameterOperationResult>>
                SetAsync(
                    IReadOnlyDictionary<string, object> parameters,
                    CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed record ParameterSetCall(
            string ParameterName,
            object Value);
    }
}