using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Application.Input;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class ManualInputCoordinatorTests
    {
        [Fact]
        public async Task FirstSource_ShouldPressActionOnce()
        {
            RecordingActionSink sink =
                new();

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await coordinator.SetActiveAsync(
                ManualInputSource.Keyboard,
                ApplicationActionIds.MotionUp,
                true);

            Assert.Single(
                sink.Pressed);

            Assert.Equal(
                ApplicationActionIds.MotionUp,
                sink.Pressed[0]);
        }

        [Fact]
        public async Task KeyboardAndGamepad_ShouldShareSameActionOwnership()
        {
            RecordingActionSink sink =
                new();

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await coordinator.SetActiveAsync(
                ManualInputSource.Keyboard,
                ApplicationActionIds.MotionUp,
                true);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionUp,
                true);

            Assert.Single(
                sink.Pressed);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionUp,
                false);

            Assert.Empty(
                sink.Released);

            await coordinator.SetActiveAsync(
                ManualInputSource.Keyboard,
                ApplicationActionIds.MotionUp,
                false);

            Assert.Single(
                sink.Released);

            Assert.Equal(
                ApplicationActionIds.MotionUp,
                sink.Released[0]);
        }

        [Fact]
        public async Task DuplicateSourceState_ShouldNotRepeatDispatch()
        {
            RecordingActionSink sink =
                new();

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionRight,
                true);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionRight,
                true);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionRight,
                true);

            Assert.Single(
                sink.Pressed);
        }

        [Fact]
        public async Task ReleaseSource_ShouldOnlyReleaseActionsOwnedByThatSource()
        {
            RecordingActionSink sink =
                new();

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await coordinator.SetActiveAsync(
                ManualInputSource.Keyboard,
                ApplicationActionIds.MotionUp,
                true);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionUp,
                true);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionRight,
                true);

            await coordinator.ReleaseSourceAsync(
                ManualInputSource.Gamepad);

            Assert.DoesNotContain(
                ApplicationActionIds.MotionUp,
                sink.Released);

            Assert.Contains(
                ApplicationActionIds.MotionRight,
                sink.Released);
        }

        [Fact]
        public async Task ReleaseAll_ShouldReleaseEveryActiveAction()
        {
            RecordingActionSink sink =
                new();

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await coordinator.SetActiveAsync(
                ManualInputSource.Keyboard,
                ApplicationActionIds.MotionUp,
                true);

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionRight,
                true);

            await coordinator.ReleaseAllAsync();

            Assert.Contains(
                ApplicationActionIds.MotionUp,
                sink.Released);

            Assert.Contains(
                ApplicationActionIds.MotionRight,
                sink.Released);
        }

        [Fact]
        public async Task AmbiguousPressFailure_ShouldStillAttemptReleaseLater()
        {
            RecordingActionSink sink =
                new()
                {
                    FailNextPress =
                        true
                };

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    coordinator.SetActiveAsync(
                        ManualInputSource.Gamepad,
                        ApplicationActionIds.MotionLeft,
                        true));

            await coordinator.SetActiveAsync(
                ManualInputSource.Gamepad,
                ApplicationActionIds.MotionLeft,
                false);

            Assert.Contains(
                ApplicationActionIds.MotionLeft,
                sink.Released);
        }

        [Fact]
        public async Task UnsupportedAction_ShouldBeRejected()
        {
            RecordingActionSink sink =
                new();

            using ManualInputCoordinator coordinator =
                new(
                    sink);

            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    coordinator.SetActiveAsync(
                        ManualInputSource.Gamepad,
                        GamepadActionIds.WeaponFire,
                        true));
        }

        private sealed class RecordingActionSink :
            IManualInputActionSink
        {
            public List<string> Pressed
            {
                get;
            } =
                new();

            public List<string> Released
            {
                get;
            } =
                new();

            public bool FailNextPress
            {
                get;
                set;
            }

            public Task PressAsync(
                string actionId,
                CancellationToken cancellationToken = default)
            {
                Pressed.Add(
                    actionId);

                if (FailNextPress)
                {
                    FailNextPress =
                        false;

                    throw new InvalidOperationException(
                        "Simulated ambiguous press failure.");
                }

                return Task.CompletedTask;
            }

            public Task ReleaseAsync(
                string actionId,
                CancellationToken cancellationToken = default)
            {
                Released.Add(
                    actionId);

                return Task.CompletedTask;
            }
        }
    }
}