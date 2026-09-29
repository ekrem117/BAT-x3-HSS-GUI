namespace BatX3_HSS_GUI.Application.Gamepad
{
    public static class GamepadSettingsComparer
    {
        public static bool AreEquivalent(
            GamepadSettings left,
            GamepadSettings right)
        {
            ArgumentNullException.ThrowIfNull(
                left);

            ArgumentNullException.ThrowIfNull(
                right);

            if (ReferenceEquals(
                    left,
                    right))
            {
                return true;
            }

            if (left.Enabled !=
                    right.Enabled ||
                !string.Equals(
                    left.ActiveDeviceId,
                    right.ActiveDeviceId,
                    StringComparison.Ordinal) ||
                left.InvertY !=
                    right.InvertY ||
                left.Deadzone !=
                    right.Deadzone ||
                left.TriggerThreshold !=
                    right.TriggerThreshold ||
                left.TriggerDebounceSamples !=
                    right.TriggerDebounceSamples ||
                left.PollingIntervalMs !=
                    right.PollingIntervalMs ||
                left.FireMinimumIntervalMs !=
                    right.FireMinimumIntervalMs)
            {
                return false;
            }

            if (left.Bindings.Count !=
                right.Bindings.Count)
            {
                return false;
            }

            GamepadBindingSettings[] leftBindings =
                left.Bindings
                    .OrderBy(
                        binding =>
                            binding.ActionId,
                        StringComparer.Ordinal)
                    .ThenBy(
                        binding =>
                            binding.Control)
                    .ToArray();

            GamepadBindingSettings[] rightBindings =
                right.Bindings
                    .OrderBy(
                        binding =>
                            binding.ActionId,
                        StringComparer.Ordinal)
                    .ThenBy(
                        binding =>
                            binding.Control)
                    .ToArray();

            for (
                int index = 0;
                index < leftBindings.Length;
                index++)
            {
                if (!string.Equals(
                        leftBindings[index].ActionId,
                        rightBindings[index].ActionId,
                        StringComparison.Ordinal) ||
                    leftBindings[index].Control !=
                        rightBindings[index].Control)
                {
                    return false;
                }
            }

            return true;
        }
    }
}