namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadControlEdgeProcessor
    {
        private HashSet<GamepadControl>
            _previousControls =
                new();

        private bool _rightTriggerState;
        private bool _rightTriggerCandidate;
        private int _rightTriggerStableSamples;

        private bool _leftTriggerState;
        private bool _leftTriggerCandidate;
        private int _leftTriggerStableSamples;

        public void Reset()
        {
            _previousControls.Clear();

            _rightTriggerState =
                false;

            _rightTriggerCandidate =
                false;

            _rightTriggerStableSamples =
                0;

            _leftTriggerState =
                false;

            _leftTriggerCandidate =
                false;

            _leftTriggerStableSamples =
                0;
        }

        public GamepadControlTransitions Process(
            GamepadReadingSnapshot snapshot,
            GamepadSettings settings)
        {
            ArgumentNullException.ThrowIfNull(
                snapshot);

            ArgumentNullException.ThrowIfNull(
                settings);

            HashSet<GamepadControl> currentControls =
                new(
                    snapshot.PressedControls);

            _rightTriggerState =
                ProcessTrigger(
                    Math.Clamp(
                        snapshot.RightTrigger,
                        0.0,
                        1.0),
                    settings.TriggerThreshold,
                    settings.TriggerDebounceSamples,
                    _rightTriggerState,
                    ref _rightTriggerCandidate,
                    ref _rightTriggerStableSamples);

            _leftTriggerState =
                ProcessTrigger(
                    Math.Clamp(
                        snapshot.LeftTrigger,
                        0.0,
                        1.0),
                    settings.TriggerThreshold,
                    settings.TriggerDebounceSamples,
                    _leftTriggerState,
                    ref _leftTriggerCandidate,
                    ref _leftTriggerStableSamples);

            if (_rightTriggerState)
            {
                currentControls.Add(
                    GamepadControl.RightTrigger);
            }

            if (_leftTriggerState)
            {
                currentControls.Add(
                    GamepadControl.LeftTrigger);
            }

            HashSet<GamepadControl> pressed =
                currentControls
                    .Except(
                        _previousControls)
                    .ToHashSet();

            HashSet<GamepadControl> released =
                _previousControls
                    .Except(
                        currentControls)
                    .ToHashSet();

            _previousControls =
                currentControls;

            return new GamepadControlTransitions(
                pressed,
                released);
        }

        private static bool ProcessTrigger(
            double value,
            double threshold,
            int requiredStableSamples,
            bool stableState,
            ref bool candidateState,
            ref int candidateSamples)
        {
            bool currentCandidate =
                value >= threshold;

            if (currentCandidate ==
                candidateState)
            {
                candidateSamples++;
            }
            else
            {
                candidateState =
                    currentCandidate;

                candidateSamples =
                    1;
            }

            if (candidateSamples <
                requiredStableSamples)
            {
                return stableState;
            }

            return candidateState;
        }
    }
}