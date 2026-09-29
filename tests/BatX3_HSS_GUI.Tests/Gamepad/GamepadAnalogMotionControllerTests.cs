using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadAnalogMotionControllerTests
    {
        [Fact]
        public async Task NonManualMode_ShouldRejectAnalogMovement()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Idle);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            bool accepted =
                await controller.ApplyAsync(
                    new GamepadAnalogState(
                        true,
                        0.5,
                        0.5));

            Assert.False(
                accepted);

            Assert.Empty(
                parameterService.SetRequests);
        }

        [Fact]
        public async Task ManualMode_ShouldSendPanAndTilt()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Manual);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            bool accepted =
                await controller.ApplyAsync(
                    new GamepadAnalogState(
                        true,
                        0.5,
                        -0.25));

            Assert.True(
                accepted);

            IReadOnlyDictionary<string, object> request =
                Assert.Single(
                    parameterService.SetRequests);

            Assert.Equal(
                0.5,
                request[
                    ParameterNames.Motion.AnalogPan]);

            Assert.Equal(
                -0.25,
                request[
                    ParameterNames.Motion.AnalogTilt]);
        }

        [Fact]
        public async Task SmallAnalogChange_ShouldBeSuppressed()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Manual);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            await controller.ApplyAsync(
                new GamepadAnalogState(
                    true,
                    0.500,
                    0.000));

            await controller.ApplyAsync(
                new GamepadAnalogState(
                    true,
                    0.505,
                    0.000));

            Assert.Single(
                parameterService.SetRequests);
        }

        [Fact]
        public async Task ReturnToZero_ShouldAlwaysBeSent()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Manual);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            await controller.ApplyAsync(
                new GamepadAnalogState(
                    true,
                    0.005,
                    0.0));

            await controller.ApplyAsync(
                new GamepadAnalogState(
                    true,
                    0.0,
                    0.0));

            Assert.Equal(
                2,
                parameterService.SetRequests.Count);

            Assert.Equal(
                0.0,
                parameterService.SetRequests[1][
                    ParameterNames.Motion.AnalogPan]);
        }

        [Fact]
        public async Task Reset_ShouldAlwaysSendAllFailSafeValues()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Manual);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            await controller.ResetAsync();

            await controller.ResetAsync();

            Assert.Equal(
                2,
                parameterService.SetRequests.Count);

            foreach (
                IReadOnlyDictionary<string, object> request
                in parameterService.SetRequests)
            {
                Assert.Equal(
                    3,
                    request.Count);

                Assert.Equal(
                    0.0,
                    request[
                        ParameterNames.Motion.AnalogPan]);

                Assert.Equal(
                    0.0,
                    request[
                        ParameterNames.Motion.AnalogTilt]);

                Assert.Equal(
                    0,
                    request[
                        ParameterNames.Motion.AnalogPrecision]);
            }
        }

        [Fact]
        public async Task Precision_ShouldSendOneAndZero()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Manual);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            Assert.True(
                await controller.SetPrecisionAsync(
                    true));

            Assert.True(
                await controller.SetPrecisionAsync(
                    false));

            Assert.Equal(
                2,
                parameterService.SetRequests.Count);

            Assert.Equal(
                1,
                parameterService.SetRequests[0][
                    ParameterNames.Motion.AnalogPrecision]);

            Assert.Equal(
                0,
                parameterService.SetRequests[1][
                    ParameterNames.Motion.AnalogPrecision]);
        }

        [Fact]
        public async Task PrecisionEnableOutsideManual_ShouldBeRejected()
        {
            RecordingParameterService parameterService =
                new();

            FakeSystemRuntimeState runtimeState =
                new(
                    SystemOperatingMode.Idle);

            using GamepadAnalogMotionController controller =
                new(
                    parameterService,
                    runtimeState);

            bool accepted =
                await controller.SetPrecisionAsync(
                    true);

            Assert.False(
                accepted);

            Assert.Empty(
                parameterService.SetRequests);
        }

        private sealed class FakeSystemRuntimeState :
            ISystemRuntimeState
        {
            public FakeSystemRuntimeState(
                SystemOperatingMode? mode)
            {
                CurrentMode =
                    mode;
            }

            public SystemOperatingMode? CurrentMode
            {
                get;
                private set;
            }

            public void UpdateMode(
                SystemOperatingMode mode)
            {
                CurrentMode =
                    mode;
            }

            public void MarkUnknown()
            {
                CurrentMode =
                    null;
            }
        }

        private sealed class RecordingParameterService :
            IParameterService
        {
            public List<
                IReadOnlyDictionary<string, object>>
                SetRequests
            {
                get;
            } =
                new();

            public Task<ParameterOperationResult> GetAsync(
                string parameterName,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<
                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult>>
                GetAsync(
                    IReadOnlyCollection<string> parameterNames,
                    CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public async Task<ParameterOperationResult> SetAsync(
                string parameterName,
                object value,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await SetAsync(
                            new Dictionary<string, object>
                            {
                                [parameterName] =
                                    value
                            },
                            cancellationToken);

                return results[
                    parameterName];
            }

            public Task<
                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult>>
                SetAsync(
                    IReadOnlyDictionary<string, object> parameters,
                    CancellationToken cancellationToken = default)
            {
                Dictionary<string, object> captured =
                    parameters.ToDictionary(
                        item =>
                            item.Key,
                        item =>
                            item.Value,
                        StringComparer.Ordinal);

                SetRequests.Add(
                    captured);

                Dictionary<
                    string,
                    ParameterOperationResult> results =
                        new(
                            StringComparer.Ordinal);

                foreach (
                    KeyValuePair<string, object> parameter
                    in parameters)
                {
                    results[
                        parameter.Key] =
                        new ParameterOperationResult(
                            parameter.Key,
                            default,
                            parameter.Value);
                }

                return Task.FromResult<
                    IReadOnlyDictionary<
                        string,
                        ParameterOperationResult>>(
                            results);
            }
        }
    }
}