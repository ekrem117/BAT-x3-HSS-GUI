using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadAnalogAxisProcessorTests
    {
        [Fact]
        public void DisplacedStickAtStartup_ShouldRequireNeutral()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                new();

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            1.0
                    },
                    settings);

            Assert.False(
                state.IsReady);

            Assert.Equal(
                0.0,
                state.Pan);

            Assert.Equal(
                0.0,
                state.Tilt);
        }

        [Fact]
        public void NeutralSample_ShouldArmProcessorWithoutMovement()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                new();

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot(),
                    settings);

            Assert.True(
                state.IsReady);

            Assert.Equal(
                0.0,
                state.Pan);

            Assert.Equal(
                0.0,
                state.Tilt);

            Assert.False(
                processor.NeutralRequired);
        }

        [Fact]
        public void ValueInsideDeadzone_ShouldBeZero()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            0.10,

                        LeftStickY =
                            -0.10
                    },
                    settings);

            Assert.Equal(
                0.0,
                state.Pan);

            Assert.Equal(
                0.0,
                state.Tilt);
        }

        [Fact]
        public void FullPositiveX_ShouldProduceFullPositivePan()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            1.0
                    },
                    settings);

            Assert.Equal(
                1.0,
                state.Pan,
                precision: 6);
        }

        [Fact]
        public void FullNegativeX_ShouldProduceFullNegativePan()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            -1.0
                    },
                    settings);

            Assert.Equal(
                -1.0,
                state.Pan,
                precision: 6);
        }

        [Fact]
        public void DeadzoneBoundary_ShouldRescaleRemainingRange()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            /*
             * deadzone = 0.15
             * raw      = 0.575
             *
             * (0.575 - 0.15) / 0.85 = 0.5
             */
            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            0.575
                    },
                    settings);

            Assert.Equal(
                0.5,
                state.Pan,
                precision: 6);
        }

        [Fact]
        public void InvertY_ShouldReverseTilt()
        {
            GamepadSettings settings =
                CreateSettings();

            settings.InvertY =
                true;

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickY =
                            1.0
                    },
                    settings);

            Assert.Equal(
                -1.0,
                state.Tilt,
                precision: 6);
        }

        [Fact]
        public void WithoutInvertY_PositiveY_ShouldRemainPositiveTilt()
        {
            GamepadSettings settings =
                CreateSettings();

            settings.InvertY =
                false;

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickY =
                            1.0
                    },
                    settings);

            Assert.Equal(
                1.0,
                state.Tilt,
                precision: 6);
        }

        [Fact]
        public void DiagonalInput_ShouldPreserveBothAxes()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            1.0,

                        LeftStickY =
                            1.0
                    },
                    settings);

            Assert.Equal(
                1.0,
                state.Pan,
                precision: 6);

            Assert.Equal(
                1.0,
                state.Tilt,
                precision: 6);
        }

        [Fact]
        public void Reset_ShouldRequireNeutralAgain()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            processor.Process(
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        1.0
                },
                settings);

            processor.Reset();

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            1.0
                    },
                    settings);

            Assert.False(
                state.IsReady);

            Assert.Equal(
                0.0,
                state.Pan);
        }

        [Fact]
        public void OutOfRangeInput_ShouldBeClamped()
        {
            GamepadSettings settings =
                CreateSettings();

            GamepadAnalogAxisProcessor processor =
                CreateReadyProcessor(
                    settings);

            GamepadAnalogState state =
                processor.Process(
                    new GamepadReadingSnapshot
                    {
                        LeftStickX =
                            5.0,

                        LeftStickY =
                            -5.0
                    },
                    settings);

            Assert.Equal(
                1.0,
                state.Pan,
                precision: 6);

            Assert.Equal(
                -1.0,
                state.Tilt,
                precision: 6);
        }

        private static GamepadSettings CreateSettings()
        {
            return new GamepadSettings
            {
                Deadzone =
                    0.15,

                InvertY =
                    false
            };
        }

        private static GamepadAnalogAxisProcessor CreateReadyProcessor(
            GamepadSettings settings)
        {
            GamepadAnalogAxisProcessor processor =
                new();

            GamepadAnalogState initialState =
                processor.Process(
                    new GamepadReadingSnapshot(),
                    settings);

            Assert.True(
                initialState.IsReady);

            return processor;
        }
    }
}