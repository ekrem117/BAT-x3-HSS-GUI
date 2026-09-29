namespace BatX3_HSS_GUI.Application.Gamepad
{
    public static class GamepadActionIds
    {
        public const string MotionPrecision =
            "Motion.Precision";

        public const string WeaponFire =
            "Weapon.Fire";

        public const string WeaponArmToggle =
            "Weapon.ArmToggle";

        public const string ModeManual =
            "Mode.Manual";

        public const string ModeIdle =
            "Mode.Idle";

        public const string ModeEmergencyStop =
            "Mode.EmergencyStop";

        public static IReadOnlySet<string> Supported
        {
            get;
        } =
            new HashSet<string>(
                StringComparer.Ordinal)
            {
                MotionPrecision,
                WeaponFire,
                WeaponArmToggle,
                ModeManual,
                ModeIdle,
                ModeEmergencyStop
            };
    }
}