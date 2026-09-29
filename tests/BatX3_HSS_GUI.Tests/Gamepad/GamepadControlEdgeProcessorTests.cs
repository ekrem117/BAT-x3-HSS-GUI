using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadControlEdgeProcessorTests
    {
        [Fact]
        public void ButtonHeld_ShouldProduceOnlyOnePressedEdge()
        {
            GamepadSettings settings =
                new();

            GamepadControlEdgeProcessor processor =
                new();

            GamepadReadingSnapshot pressedSnapshot =
                new()
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.RightShoulder
                        }
                };

            GamepadControlTransitions first =
                processor.Process(
                    pressedSnapshot,
                    settings);

            GamepadControlTransitions second =
                processor.Process(
                    pressedSnapshot,
                    settings);

            Assert.Contains(
                GamepadControl.RightShoulder,
                first.Pressed);

            Assert.Empty(
                second.Pressed);
        }

        [Fact]
        public void ButtonRelease_ShouldProduceReleasedEdge()
        {
            GamepadSettings settings =
                new();

            GamepadControlEdgeProcessor processor =
                new();

            processor.Process(
                new GamepadReadingSnapshot
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.B
                        }
                },
                settings);

            GamepadControlTransitions released =
                processor.Process(
                    new GamepadReadingSnapshot(),
                    settings);

            Assert.Contains(
                GamepadControl.B,
                released.Released);
        }

        [Fact]
        public void Trigger_ShouldRespectDebounce()
        {
            GamepadSettings settings =
                new()
                {
                    TriggerThreshold =
                        0.50,

                    TriggerDebounceSamples =
                        3
                };

            GamepadControlEdgeProcessor processor =
                new();

            GamepadReadingSnapshot pressed =
                new()
                {
                    RightTrigger =
                        1.0
                };

            GamepadControlTransitions first =
                processor.Process(
                    pressed,
                    settings);

            GamepadControlTransitions second =
                processor.Process(
                    pressed,
                    settings);

            GamepadControlTransitions third =
                processor.Process(
                    pressed,
                    settings);

            Assert.DoesNotContain(
                GamepadControl.RightTrigger,
                first.Pressed);

            Assert.DoesNotContain(
                GamepadControl.RightTrigger,
                second.Pressed);

            Assert.Contains(
                GamepadControl.RightTrigger,
                third.Pressed);
        }

        [Fact]
        public void Reset_ShouldClearPreviousButtonState()
        {
            GamepadSettings settings =
                new();

            GamepadControlEdgeProcessor processor =
                new();

            GamepadReadingSnapshot pressed =
                new()
                {
                    PressedControls =
                        new HashSet<GamepadControl>
                        {
                            GamepadControl.RightShoulder
                        }
                };

            processor.Process(
                pressed,
                settings);

            processor.Reset();

            GamepadControlTransitions afterReset =
                processor.Process(
                    pressed,
                    settings);

            Assert.Contains(
                GamepadControl.RightShoulder,
                afterReset.Pressed);
        }
    }
}