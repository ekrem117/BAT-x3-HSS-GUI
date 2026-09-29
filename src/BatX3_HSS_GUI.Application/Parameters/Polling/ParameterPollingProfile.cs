namespace BatX3_HSS_GUI.Application.Parameters.Polling
{
    public static class ParameterPollingProfile
    {
        public static IReadOnlyList<string> StatusParameters { get; } =
        [
            ParameterNames.System.Mode,
            ParameterNames.System.Link,
            ParameterNames.System.Fps,

            ParameterNames.Vision.ConfidenceThreshold,
            ParameterNames.Vision.DetectionCount,
            ParameterNames.Vision.InferenceMilliseconds,

            ParameterNames.Weapon.Armed,
            ParameterNames.Weapon.FireMode,
            ParameterNames.Weapon.BurstCount,
            ParameterNames.Weapon.Selected,
            ParameterNames.Weapon.Ammo,
            ParameterNames.Weapon.ShotsFired
        ];
    }
}