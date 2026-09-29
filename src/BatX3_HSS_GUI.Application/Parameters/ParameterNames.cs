namespace BatX3_HSS_GUI.Application.Parameters
{
    public static class ParameterNames
    {
        public static class Vision
        {
            public const string EnemyHMin =
                "vision.enemy.h_min";

            public const string EnemyHMax =
                "vision.enemy.h_max";

            public const string EnemyHMin2 =
                "vision.enemy.h_min2";

            public const string EnemyHMax2 =
                "vision.enemy.h_max2";

            public const string EnemySMin =
                "vision.enemy.s_min";

            public const string EnemyVMin =
                "vision.enemy.v_min";

            public const string FriendHMin =
                "vision.friend.h_min";

            public const string FriendHMax =
                "vision.friend.h_max";

            public const string FriendSMin =
                "vision.friend.s_min";

            public const string FriendVMin =
                "vision.friend.v_min";

            public const string ConfidenceThreshold =
                "vision.conf_threshold";

            public const string DetectionCount =
                "vision.detection_count";

            public const string InferenceMilliseconds =
                "vision.inference_ms";
        }

        public static class Control
        {
            public const string PidKp =
                "control.pid.kp";
        }

        public static class System
        {
            public const string Mode =
                "system.mode";

            public const string Link =
                "system.link";

            public const string Fps =
                "system.fps";
        }

        public static class Motion
        {
            public const string ManualUp =
                "motion.manual.up";

            public const string ManualDown =
                "motion.manual.down";

            public const string ManualLeft =
                "motion.manual.left";

            public const string ManualRight =
                "motion.manual.right";

            public const string AnalogPan =
                "motion.analog.pan";

            public const string AnalogTilt =
                "motion.analog.tilt";

            public const string AnalogPrecision =
                "motion.analog.precision";

            public const string AutoPan =
                "motion.auto.pan";

            public const string AutoTilt =
                "motion.auto.tilt";
        }

        public static class Weapon
        {
            public const string Armed =
                "weapon.armed";

            public const string FireMode =
                "weapon.fire_mode";

            public const string BurstCount =
                "weapon.burst_count";

            public const string Selected =
                "weapon.selected";

            public const string Ammo =
                "weapon.ammo";

            public const string ShotsFired =
                "weapon.shots_fired";

            public const string Fire =
                "weapon.fire";
        }
    }
}