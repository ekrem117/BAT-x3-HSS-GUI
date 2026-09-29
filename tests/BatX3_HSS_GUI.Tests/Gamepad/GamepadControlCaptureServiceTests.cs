using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadControlCaptureServiceTests
    {
        [Fact]
        public async Task HeldButtonAtCaptureStart_ShouldRequireNeutral()
        {
            GamepadControlCaptureService service =
                new();

            GamepadSettings settings =
                new();

            Task<GamepadControl?> captureTask =
                service.CaptureAsync(
                    null);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.X
                        }
                },
                settings);

            Assert.False(
                captureTask.IsCompleted);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot(),
                settings);

            Assert.False(
                captureTask.IsCompleted);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.X
                        }
                },
                settings);

            GamepadControl? captured =
                await captureTask;

            Assert.Equal(
                GamepadControl.X,
                captured);
        }

        [Fact]
        public async Task Trigger_ShouldRespectDebounceBeforeCapture()
        {
            GamepadControlCaptureService service =
                new();

            GamepadSettings settings =
                new()
                {
                    TriggerThreshold =
                        0.50,

                    TriggerDebounceSamples =
                        3
                };

            Task<GamepadControl?> captureTask =
                service.CaptureAsync(
                    null);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot(),
                settings);

            for (int index = 0;
                 index < 2;
                 index++)
            {
                service.ProcessReading(
                    "A",
                    new GamepadReadingSnapshot
                    {
                        RightTrigger =
                            1.0
                    },
                    settings);
            }

            Assert.False(
                captureTask.IsCompleted);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    RightTrigger =
                        1.0
                },
                settings);

            Assert.Equal(
                GamepadControl.RightTrigger,
                await captureTask);
        }

        [Fact]
        public async Task RequestedDevice_ShouldIgnoreOtherDevice()
        {
            GamepadControlCaptureService service =
                new();

            GamepadSettings settings =
                new();

            Task<GamepadControl?> captureTask =
                service.CaptureAsync(
                    "B");

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot(),
                settings);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.Y
                        }
                },
                settings);

            Assert.False(
                captureTask.IsCompleted);

            service.ProcessReading(
                "B",
                new GamepadReadingSnapshot(),
                settings);

            service.ProcessReading(
                "B",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.Y
                        }
                },
                settings);

            Assert.Equal(
                GamepadControl.Y,
                await captureTask);
        }

        [Fact]
        public async Task MultipleControls_ShouldRequireNeutralAgain()
        {
            GamepadControlCaptureService service =
                new();

            GamepadSettings settings =
                new();

            Task<GamepadControl?> captureTask =
                service.CaptureAsync(
                    null);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot(),
                settings);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.A,
                            GamepadControl.B
                        }
                },
                settings);

            Assert.False(
                captureTask.IsCompleted);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.A
                        }
                },
                settings);

            Assert.False(
                captureTask.IsCompleted);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot(),
                settings);

            service.ProcessReading(
                "A",
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.A
                        }
                },
                settings);

            Assert.Equal(
                GamepadControl.A,
                await captureTask);
        }

        [Fact]
        public async Task Cancel_ShouldCompleteWithNullAndAdvanceVersion()
        {
            GamepadControlCaptureService service =
                new();

            long initialVersion =
                service.GetSnapshot()
                    .Version;

            Task<GamepadControl?> captureTask =
                service.CaptureAsync(
                    null);

            long captureVersion =
                service.GetSnapshot()
                    .Version;

            Assert.True(
                captureVersion >
                initialVersion);

            service.Cancel();

            GamepadControl? result =
                await captureTask;

            Assert.Null(
                result);

            GamepadControlCaptureSnapshot snapshot =
                service.GetSnapshot();

            Assert.False(
                snapshot.IsActive);

            Assert.True(
                snapshot.Version >
                captureVersion);
        }
    }
}
