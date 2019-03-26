#if GOOGLEVR

using Juniper.Haptics;
using UnityEngine;

using InputButton = UnityEngine.EventSystems.PointerEventData.InputButton;

namespace Juniper.Unity.Input.Pointers.Motion
{
    public class DaydreamProbeConfiguration : AbstractMotionControllerConfiguration<GvrControllerHand>
    {
        public DaydreamProbeConfiguration() :
            base(GvrControllerHand.Left, GvrControllerHand.Right) { }
    }

    /// <summary>
    /// A motion controller or hand-tracking. Currently only implements WindowsMR.
    /// </summary>
    public abstract class DaydreamMotionController
        : AbstractMotionController<GvrControllerHand, GvrControllerButton, DaydreamProbeConfiguration, NoHaptics>
    {
        public override bool IsConnected =>
            GvrControllerInput.GetDevice(NativeHandID)?.State == GvrConnectionState.Connected;

        private GvrTrackedController controller;

        public override void Awake()
        {
            base.Awake();

            AddButton(VirtualTouchPadButton.Top, InputButton.Left);
            AddButton(VirtualTouchPadButton.Bottom, InputButton.Right);
            AddButton(GvrControllerButton.App, InputButton.Middle);
        }

        public override bool Install(bool reset)
        {
            if(base.Install(reset))
            {
                var arm = this.Ensure<GvrArmModel>().Value;
                controller = this.Ensure<GvrTrackedController>();
                controller.ControllerHand = NativeHandID;
                controller.ArmModel = arm;
                return true;
            }

            return false;
        }

        public override void Uninstall()
        {
            base.Uninstall();

            this.Remove<GvrTrackedController>();
            this.Remove<GvrArmModel>();
        }

        public override void UpdatePointer()
        {
            controller.ControllerHand = NativeHandID;
            base.UpdatePointer();
        }

        private GvrControllerInputDevice Device =>
            controller?.ControllerInputDevice;

        public override Vector2 RoundTouchPoint =>
            Device.TouchPos;

        public override Vector2 SquareTouchPoint =>
            RoundTouchPoint.Round2Square();

        public override bool? IsCharging =>
            Device.IsCharging;

        public override float? BatteryLevel
        {
            get
            {
                switch (Device?.BatteryLevel)
                {
                    case GvrControllerBatteryLevel.Full:
                        return 1.0f;

                    case GvrControllerBatteryLevel.AlmostFull:
                        return 0.8f;

                    case GvrControllerBatteryLevel.Medium:
                        return 0.6f;

                    case GvrControllerBatteryLevel.Low:
                        return 0.4f;

                    case GvrControllerBatteryLevel.CriticalLow:
                        return 0.2f;

                    default:
                        return 0.0f;
                }
            }
        }

        public override bool IsDominantHand =>
            Device.IsDominantHand;

        public override bool IsLeftHand =>
            NativeHandID == GvrControllerHand.Left;

        public override bool IsRightHand =>
            NativeHandID == GvrControllerHand.Right;

        protected override bool TouchPadTouched =>
            Device.GetButton(GvrControllerButton.TouchPadTouch);

        protected override bool TouchPadTouchedDown =>
            Device.GetButtonDown(GvrControllerButton.TouchPadTouch);

        protected override bool TouchPadTouchedUp =>
            Device.GetButtonUp(GvrControllerButton.TouchPadTouch);

        protected override bool TouchPadPressed =>
            Device.GetButton(GvrControllerButton.TouchPadButton);

        protected override bool TouchPadPressedDown =>
            Device.GetButtonDown(GvrControllerButton.TouchPadButton);

        protected override bool TouchPadPressedUp =>
            Device.GetButtonUp(GvrControllerButton.TouchPadButton);

        public override bool IsButtonPressed(GvrControllerButton button) =>
            Device.GetButton(button);

        public override bool IsButtonUp(GvrControllerButton button) =>
            Device.GetButtonUp(button);

        public override bool IsButtonDown(GvrControllerButton button) =>
            Device.GetButtonDown(button);
    }
}

#endif