using BatX3_HSS_GUI.Application.Gamepad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace BatX3_HSS_GUI.Client.Input.Gamepad
{
    internal sealed class Gp307RawGamepadProfile :
            IRawGamepadProfile
    {
        public const ushort VendorId =
            0x0079;

        public const ushort ProductId =
            0x0006;

        private const int ExpectedAxisCount =
            4;

        private const int ExpectedButtonCount =
            12;

        private const int ExpectedSwitchCount =
            1;

        private const int LeftStickXIndex =
            0;

        private const int LeftStickYIndex =
            1;

        private const int RightStickXIndex =
            2;

        private const int RightStickYIndex =
            3;

        private const int DPadSwitchIndex =
            0;

        private const int FaceButton1Index =
            0;

        private const int FaceButton2Index =
            1;

        private const int FaceButton3Index =
            2;

        private const int FaceButton4Index =
            3;

        private const int LeftShoulderIndex =
            4;

        private const int RightShoulderIndex =
            5;

        private const int LeftTriggerIndex =
            6;

        private const int RightTriggerIndex =
            7;

        private const int ViewIndex =
            8;

        private const int MenuIndex =
            9;

        private const int LeftThumbstickIndex =
            10;

        private const int RightThumbstickIndex =
            11;

        public static Gp307RawGamepadProfile Instance
        {
            get;
        } =
            new();

        private Gp307RawGamepadProfile()
        {
        }

        public string DisplayName =>
            "GameMaster GP-307 (USB Joystick)";

        public bool IsMatch(
            RawGameController controller)
        {
            ArgumentNullException.ThrowIfNull(
                controller);

            return
                controller.HardwareVendorId ==
                    VendorId &&
                controller.HardwareProductId ==
                    ProductId &&
                controller.AxisCount ==
                    ExpectedAxisCount &&
                controller.ButtonCount ==
                    ExpectedButtonCount &&
                controller.SwitchCount ==
                    ExpectedSwitchCount;
        }

        public bool TryRead(
            RawGameController controller,
            out GamepadReadingSnapshot? snapshot)
        {
            ArgumentNullException.ThrowIfNull(
                controller);

            if (!IsMatch(
                    controller))
            {
                snapshot =
                    null;

                return false;
            }

            bool[] buttons =
                new bool[
                    controller.ButtonCount];

            GameControllerSwitchPosition[] switches =
                new GameControllerSwitchPosition[
                    controller.SwitchCount];

            double[] axes =
                new double[
                    controller.AxisCount];

            controller.GetCurrentReading(
                buttons,
                switches,
                axes);

            return TryMapReading(
                buttons,
                switches,
                axes,
                out snapshot);
        }

        internal static bool TryMapReading(
            bool[] buttons,
            GameControllerSwitchPosition[] switches,
            double[] axes,
            out GamepadReadingSnapshot? snapshot)
        {
            ArgumentNullException.ThrowIfNull(
                buttons);

            ArgumentNullException.ThrowIfNull(
                switches);

            ArgumentNullException.ThrowIfNull(
                axes);

            if (buttons.Length <
                    ExpectedButtonCount ||
                switches.Length <
                    ExpectedSwitchCount ||
                axes.Length <
                    ExpectedAxisCount)
            {
                snapshot =
                    null;

                return false;
            }

            HashSet<GamepadControl> pressedControls =
                new();

            AddButtonIfPressed(
                buttons,
                FaceButton1Index,
                GamepadControl.A,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                FaceButton2Index,
                GamepadControl.B,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                FaceButton3Index,
                GamepadControl.X,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                FaceButton4Index,
                GamepadControl.Y,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                LeftShoulderIndex,
                GamepadControl.LeftShoulder,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                RightShoulderIndex,
                GamepadControl.RightShoulder,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                ViewIndex,
                GamepadControl.View,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                MenuIndex,
                GamepadControl.Menu,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                LeftThumbstickIndex,
                GamepadControl.LeftThumbstick,
                pressedControls);

            AddButtonIfPressed(
                buttons,
                RightThumbstickIndex,
                GamepadControl.RightThumbstick,
                pressedControls);

            AddDPadControls(
                switches[DPadSwitchIndex],
                pressedControls);

            /*
             * GP-307 L2/R2 fiziksel testte analog axis değil,
             * digital button olarak raporlandı.
             *
             * Üst katmandaki mevcut trigger threshold /
             * debounce davranışını korumak için bunları
             * 0.0 veya 1.0 trigger değeri haline getiriyoruz.
             */
            double leftTrigger =
                buttons[LeftTriggerIndex]
                    ? 1.0
                    : 0.0;

            double rightTrigger =
                buttons[RightTriggerIndex]
                    ? 1.0
                    : 0.0;

            snapshot =
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        NormalizeHorizontalAxis(
                            axes[LeftStickXIndex]),

                    LeftStickY =
                        NormalizeVerticalAxis(
                            axes[LeftStickYIndex]),

                    LeftTrigger =
                        leftTrigger,

                    RightTrigger =
                        rightTrigger,

                    PressedControls =
                        pressedControls
                };

            /*
             * A2 / A3 fiziksel olarak sağ stick olarak
             * doğrulandı:
             *
             * RightStickXIndex = 2
             * RightStickYIndex = 3
             *
             * Ancak mevcut GamepadReadingSnapshot V1 contract'ında
             * right-stick alanları bulunmadığı için şu anda
             * bilinçli olarak üst katmana aktarılmıyor.
             */
            _ =
                axes[RightStickXIndex];

            _ =
                axes[RightStickYIndex];

            return true;
        }

        private static double NormalizeHorizontalAxis(
            double rawValue)
        {
            double clamped =
                Math.Clamp(
                    rawValue,
                    0.0,
                    1.0);

            return Math.Clamp(
                (clamped * 2.0) - 1.0,
                -1.0,
                1.0);
        }

        private static double NormalizeVerticalAxis(
            double rawValue)
        {
            double clamped =
                Math.Clamp(
                    rawValue,
                    0.0,
                    1.0);

            /*
             * GP-307 fiziksel test:
             *
             * UP   -> 0.0
             * DOWN -> 1.0
             *
             * Windows.Gaming.Input.Gamepad semantiğine
             * normalize ediyoruz:
             *
             * UP   -> +1.0
             * DOWN -> -1.0
             */
            return Math.Clamp(
                1.0 - (clamped * 2.0),
                -1.0,
                1.0);
        }

        private static void AddButtonIfPressed(
            bool[] buttons,
            int buttonIndex,
            GamepadControl control,
            ISet<GamepadControl> result)
        {
            if (buttons[buttonIndex])
            {
                result.Add(
                    control);
            }
        }

        private static void AddDPadControls(
            GameControllerSwitchPosition position,
            ISet<GamepadControl> result)
        {
            switch (position)
            {
                case GameControllerSwitchPosition.Up:
                    {
                        result.Add(
                            GamepadControl.DPadUp);

                        break;
                    }

                case GameControllerSwitchPosition.UpRight:
                    {
                        result.Add(
                            GamepadControl.DPadUp);

                        result.Add(
                            GamepadControl.DPadRight);

                        break;
                    }

                case GameControllerSwitchPosition.Right:
                    {
                        result.Add(
                            GamepadControl.DPadRight);

                        break;
                    }

                case GameControllerSwitchPosition.DownRight:
                    {
                        result.Add(
                            GamepadControl.DPadDown);

                        result.Add(
                            GamepadControl.DPadRight);

                        break;
                    }

                case GameControllerSwitchPosition.Down:
                    {
                        result.Add(
                            GamepadControl.DPadDown);

                        break;
                    }

                case GameControllerSwitchPosition.DownLeft:
                    {
                        result.Add(
                            GamepadControl.DPadDown);

                        result.Add(
                            GamepadControl.DPadLeft);

                        break;
                    }

                case GameControllerSwitchPosition.Left:
                    {
                        result.Add(
                            GamepadControl.DPadLeft);

                        break;
                    }

                case GameControllerSwitchPosition.UpLeft:
                    {
                        result.Add(
                            GamepadControl.DPadUp);

                        result.Add(
                            GamepadControl.DPadLeft);

                        break;
                    }

                case GameControllerSwitchPosition.Center:
                default:
                    {
                        break;
                    }
            }
        }
    }
}