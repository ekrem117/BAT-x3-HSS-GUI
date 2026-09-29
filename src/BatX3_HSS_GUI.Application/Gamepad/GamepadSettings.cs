namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadSettings
    {
        public bool Enabled
        {
            get;
            set;
        } =
            true;

        public string? ActiveDeviceId
        {
            get;
            set;
        }

        public bool InvertY
        {
            get;
            set;
        } =
            true;

        public double Deadzone
        {
            get;
            set;
        } =
            0.15;

        public double TriggerThreshold
        {
            get;
            set;
        } =
            0.50;

        public int TriggerDebounceSamples
        {
            get;
            set;
        } =
            3;

        public int PollingIntervalMs
        {
            get;
            set;
        } =
            10;

        public int FireMinimumIntervalMs
        {
            get;
            set;
        } =
            1000;

        public List<GamepadBindingSettings> Bindings
        {
            get;
            set;
        } =
        [
            new()
            {
                ActionId =
                    GamepadActionIds.MotionPrecision,

                Control =
                    GamepadControl.RightTrigger
            },

            new()
            {
                ActionId =
                    GamepadActionIds.WeaponFire,

                Control =
                    GamepadControl.RightShoulder
            },

            new()
            {
                ActionId =
                    GamepadActionIds.WeaponArmToggle,

                Control =
                    GamepadControl.LeftShoulder
            },

            new()
            {
                ActionId =
                    GamepadActionIds.ModeManual,

                Control =
                    GamepadControl.Menu
            },

            new()
            {
                ActionId =
                    GamepadActionIds.ModeIdle,

                Control =
                    GamepadControl.View
            },

            new()
            {
                ActionId =
                    GamepadActionIds.ModeEmergencyStop,

                Control =
                    GamepadControl.B
            }
        ];
    }
}