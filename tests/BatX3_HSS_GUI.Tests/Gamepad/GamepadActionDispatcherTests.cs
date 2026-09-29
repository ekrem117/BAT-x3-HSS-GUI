using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;
using System.Reflection;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadActionDispatcherTests
    {
        [Theory]
        [InlineData(
            GamepadActionIds.ModeManual,
            SystemOperatingMode.Manual)]
        [InlineData(
            GamepadActionIds.ModeIdle,
            SystemOperatingMode.Idle)]
        [InlineData(
            GamepadActionIds.ModeEmergencyStop,
            SystemOperatingMode.EmergencyStop)]
        public async Task ModeActions_ShouldUseSystemModeService(
            string actionId,
            SystemOperatingMode expectedMode)
        {
            TestContext context =
                CreateContext();

            await context.Dispatcher.HandlePressedAsync(
                actionId);

            SystemOperatingMode actualMode =
                Assert.Single(
                    context.SystemModeProxy.RequestedModes);

            Assert.Equal(
                expectedMode,
                actualMode);
        }

        [Fact]
        public async Task PrecisionPress_ShouldEnableAnalogPrecision()
        {
            TestContext context =
                CreateContext();

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.MotionPrecision);

            bool request =
                Assert.Single(
                    context.AnalogMotionController
                        .PrecisionRequests);

            Assert.True(
                request);
        }

        [Fact]
        public async Task PrecisionRelease_ShouldDisableAnalogPrecision()
        {
            TestContext context =
                CreateContext();

            await context.Dispatcher.HandleReleasedAsync(
                GamepadActionIds.MotionPrecision);

            bool request =
                Assert.Single(
                    context.AnalogMotionController
                        .PrecisionRequests);

            Assert.False(
                request);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        public async Task ArmToggle_ShouldReadAuthoritativeStateAndWriteOpposite(
            int currentValue,
            int expectedValue)
        {
            TestContext context =
                CreateContext();

            context.ParameterServiceProxy.GetValue =
                currentValue;

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponArmToggle);

            Assert.Single(
                context.ParameterServiceProxy.GetRequests);

            Assert.Equal(
                ParameterNames.Weapon.Armed,
                context.ParameterServiceProxy.GetRequests[0]);

            ParameterSetRequest request =
                Assert.Single(
                    context.ParameterServiceProxy.SetRequests);

            Assert.Equal(
                ParameterNames.Weapon.Armed,
                request.ParameterName);

            Assert.Equal(
                expectedValue,
                Convert.ToInt32(
                    request.Value));
        }

        [Fact]
        public async Task ArmToggle_WithUnexpectedServerValue_ShouldFailClosed()
        {
            TestContext context =
                CreateContext();

            context.ParameterServiceProxy.GetValue =
                2;

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        context.Dispatcher
                            .HandlePressedAsync(
                                GamepadActionIds
                                    .WeaponArmToggle));

            Assert.Empty(
                context.ParameterServiceProxy.SetRequests);
        }

        [Fact]
        public async Task FireTwiceInsideMinimumInterval_ShouldDispatchOnlyOnce()
        {
            TestContext context =
                CreateContext(
                    fireMinimumIntervalMilliseconds:
                        1000);

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            Assert.Single(
                context.ApplicationActionProxy
                    .PressedActions);

            Assert.Equal(
                GamepadActionIds.WeaponFire,
                context.ApplicationActionProxy
                    .PressedActions[0]);
        }

        [Fact]
        public async Task FireAfterMinimumInterval_ShouldDispatchAgain()
        {
            TestContext context =
                CreateContext(
                    fireMinimumIntervalMilliseconds:
                        1000);

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            context.TimeProvider.Advance(
                TimeSpan.FromMilliseconds(
                    1001));

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            Assert.Equal(
                2,
                context.ApplicationActionProxy
                    .PressedActions.Count);
        }

        [Fact]
        public async Task FailedFireAttempt_ShouldStillConsumeMinimumInterval()
        {
            TestContext context =
                CreateContext(
                    fireMinimumIntervalMilliseconds:
                        1000);

            context.ApplicationActionProxy
                .PressException =
                    new InvalidOperationException(
                        "Test fire dispatch failure.");

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        context.Dispatcher
                            .HandlePressedAsync(
                                GamepadActionIds
                                    .WeaponFire));

            /*
             * İlk denemenin sonucu belirsiz/hatalı olsa bile
             * aynı physical press penceresinde ikinci deneme
             * oluşturulmamalıdır.
             */
            context.ApplicationActionProxy
                .PressException =
                    null;

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            Assert.Single(
                context.ApplicationActionProxy
                    .PressedActions);
        }

        [Fact]
        public async Task FireWithZeroMinimumInterval_ShouldAllowSeparateEdges()
        {
            TestContext context =
                CreateContext(
                    fireMinimumIntervalMilliseconds:
                        0);

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            await context.Dispatcher.HandlePressedAsync(
                GamepadActionIds.WeaponFire);

            Assert.Equal(
                2,
                context.ApplicationActionProxy
                    .PressedActions.Count);
        }

        [Fact]
        public async Task UnsupportedAction_ShouldBeRejected()
        {
            TestContext context =
                CreateContext();

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        context.Dispatcher
                            .HandlePressedAsync(
                                "Gamepad.Unsupported"));
        }

        private static TestContext CreateContext(
            int fireMinimumIntervalMilliseconds = 1000)
        {
            IApplicationActionService
                applicationActionService =
                    CreateProxy<
                        IApplicationActionService,
                        RecordingApplicationActionProxy>(
                            out RecordingApplicationActionProxy
                                applicationActionProxy);

            ISystemModeService
                systemModeService =
                    CreateProxy<
                        ISystemModeService,
                        RecordingSystemModeProxy>(
                            out RecordingSystemModeProxy
                                systemModeProxy);

            IParameterService
                parameterService =
                    CreateProxy<
                        IParameterService,
                        RecordingParameterServiceProxy>(
                            out RecordingParameterServiceProxy
                                parameterServiceProxy);

            RecordingAnalogMotionController
                analogMotionController =
                    new();

            ManualTimeProvider timeProvider =
                new();

            GamepadSettings settings =
                new()
                {
                    FireMinimumIntervalMs =
                        fireMinimumIntervalMilliseconds
                };

            GamepadActionDispatcher dispatcher =
                new(
                    applicationActionService,
                    systemModeService,
                    parameterService,
                    analogMotionController,
                    new GamepadSettingsStore(
    settings),
                    timeProvider);

            return new TestContext(
                dispatcher,
                applicationActionProxy,
                systemModeProxy,
                parameterServiceProxy,
                analogMotionController,
                timeProvider);
        }

        private static TService CreateProxy<
            TService,
            TProxy>(
                out TProxy proxy)
            where TService :
                class
            where TProxy :
                DispatchProxy
        {
            TService service =
                DispatchProxy.Create<
                    TService,
                    TProxy>();

            proxy =
                (TProxy)(object)service;

            return service;
        }

        private sealed record TestContext(
            GamepadActionDispatcher Dispatcher,
            RecordingApplicationActionProxy
                ApplicationActionProxy,
            RecordingSystemModeProxy
                SystemModeProxy,
            RecordingParameterServiceProxy
                ParameterServiceProxy,
            RecordingAnalogMotionController
                AnalogMotionController,
            ManualTimeProvider
                TimeProvider);

        public sealed record ParameterSetRequest(
            string ParameterName,
            object Value);

        public class RecordingApplicationActionProxy :
            DispatchProxy
        {
            public List<string> PressedActions
            {
                get;
            } =
                new();

            public Exception? PressException
            {
                get;
                set;
            }

            protected override object? Invoke(
                MethodInfo? targetMethod,
                object?[]? args)
            {
                ArgumentNullException.ThrowIfNull(
                    targetMethod);

                if (string.Equals(
                        targetMethod.Name,
                        "PressAsync",
                        StringComparison.Ordinal))
                {
                    string actionId =
                        GetRequiredStringArgument(
                            args,
                            0);

                    PressedActions.Add(
                        actionId);

                    if (PressException is not null)
                    {
                        return Task.FromException(
                            PressException);
                    }

                    return Task.CompletedTask;
                }

                /*
                 * Dispatcher bu testlerde PressAsync dışındaki
                 * application-action metotlarını çağırmaz.
                 *
                 * Interface'in diğer async üyeleri fake'in
                 * derlenmesini etkilemesin.
                 */
                if (targetMethod.ReturnType ==
                    typeof(Task))
                {
                    return Task.CompletedTask;
                }

                throw new NotSupportedException(
                    $"Test proxy metodu desteklemiyor: " +
                    $"{targetMethod.Name}");
            }
        }

        public class RecordingSystemModeProxy :
            DispatchProxy
        {
            public List<SystemOperatingMode>
                RequestedModes
            {
                get;
            } =
                new();

            protected override object? Invoke(
                MethodInfo? targetMethod,
                object?[]? args)
            {
                ArgumentNullException.ThrowIfNull(
                    targetMethod);

                if (string.Equals(
                        targetMethod.Name,
                        "SetModeAsync",
                        StringComparison.Ordinal))
                {
                    if (args is null ||
                        args.Length == 0 ||
                        args[0] is not
                            SystemOperatingMode mode)
                    {
                        throw new InvalidOperationException(
                            "SetModeAsync mode argümanı bulunamadı.");
                    }

                    RequestedModes.Add(
                        mode);

                    return Task.CompletedTask;
                }

                if (targetMethod.ReturnType ==
                    typeof(Task))
                {
                    return Task.CompletedTask;
                }

                throw new NotSupportedException(
                    $"Test proxy metodu desteklemiyor: " +
                    $"{targetMethod.Name}");
            }
        }

        public class RecordingParameterServiceProxy :
            DispatchProxy
        {
            public object? GetValue
            {
                get;
                set;
            } =
                0;

            public List<string> GetRequests
            {
                get;
            } =
                new();

            public List<ParameterSetRequest> SetRequests { get; } = new();

            protected override object? Invoke(
                MethodInfo? targetMethod,
                object?[]? args)
            {
                ArgumentNullException.ThrowIfNull(
                    targetMethod);

                if (string.Equals(
                        targetMethod.Name,
                        "GetAsync",
                        StringComparison.Ordinal) &&
                    args is not null &&
                    args.Length > 0 &&
                    args[0] is string getParameterName)
                {
                    GetRequests.Add(
                        getParameterName);

                    return Task.FromResult(
                        new ParameterOperationResult(
                            getParameterName,
                            default,
                            GetValue));
                }

                if (string.Equals(
                        targetMethod.Name,
                        "SetAsync",
                        StringComparison.Ordinal) &&
                    args is not null &&
                    args.Length > 1 &&
                    args[0] is string setParameterName)
                {
                    object value =
                        args[1]
                        ?? throw new InvalidOperationException(
                            "SET değeri null olamaz.");

                    SetRequests.Add(
                        new ParameterSetRequest(
                            setParameterName,
                            value));

                    return Task.FromResult(
                        new ParameterOperationResult(
                            setParameterName,
                            default,
                            value));
                }

                throw new NotSupportedException(
                    $"Test proxy overload'u desteklemiyor: " +
                    $"{targetMethod.Name}");
            }
        }

        private sealed class
            RecordingAnalogMotionController :
                IGamepadAnalogMotionController
        {
            public List<bool> PrecisionRequests
            {
                get;
            } =
                new();

            public Task<bool> ApplyAsync(
                GamepadAnalogState state,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    true);
            }

            public Task<bool> SetPrecisionAsync(
                bool isActive,
                CancellationToken cancellationToken = default)
            {
                PrecisionRequests.Add(
                    isActive);

                return Task.FromResult(
                    true);
            }

            public Task ResetAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class ManualTimeProvider :
            TimeProvider
        {
            private long _timestamp;

            public override long TimestampFrequency =>
                TimeSpan.TicksPerSecond;

            public override long GetTimestamp()
            {
                return _timestamp;
            }

            public void Advance(
                TimeSpan duration)
            {
                if (duration <
                    TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(duration));
                }

                checked
                {
                    _timestamp +=
                        duration.Ticks;
                }
            }
        }

        private static string GetRequiredStringArgument(
            object?[]? arguments,
            int index)
        {
            if (arguments is null ||
                arguments.Length <= index ||
                arguments[index] is not string value)
            {
                throw new InvalidOperationException(
                    $"Beklenen string argüman bulunamadı. " +
                    $"Index={index}");
            }

            return value;
        }
    }
}