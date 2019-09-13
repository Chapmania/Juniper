using Juniper.Haptics;

using UnityEngine;

using UnityInput = UnityEngine.Input;

namespace Juniper.Input.Pointers.Screen
{
    /// <summary>
    /// A <see cref="AbstractScreenDevice"/> pointer for the standard mouse connected to a desktop system.
    /// </summary>
    public class Mouse : AbstractScreenDevice<KeyCode, MousePointerConfiguration>
    {
        [ContextMenu("Reinstall")]
        public override void Reinstall()
        {
            base.Reinstall();
        }

        private Vector2 MoveDelta
        {
            get
            {
                return new Vector2(UnityInput.GetAxisRaw("Mouse X"), UnityInput.GetAxisRaw("Mouse Y"));
            }
        }

        private bool mouseActive;

#if UNITY_XR_OCULUS_ANDROID
        private bool wasTouched;
        private bool wasWasTouched;
        private bool wasSwiped;
        private bool wasWasSwiped;
#endif

        public override bool IsConnected { get { return UnityInput.mousePresent && (mouseActive = mouseActive || ActiveThisFrame); } }

        public bool ActiveThisFrame
        {
            set
            {
                mouseActive = value;
            }
            get
            {
                var moved = MoveDelta.sqrMagnitude > 0;
                var pressed = IsButtonPressed(KeyCode.Mouse0);
                var platform = JuniperPlatform.CurrentPlatform;
                var system = XR.Platform.GetSystem(platform);
#if UNITY_XR_OCULUS_ANDROID && !UNITY_EDITOR
                var controller = OVRInput.GetActiveController();
                var swipe = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad | OVRInput.Axis2D.SecondaryTouchpad, controller);
                var swiped = swipe.sqrMagnitude > 0;
                var touched = OVRInput.Get(OVRInput.Touch.PrimaryTouchpad | OVRInput.Touch.SecondaryTouchpad, controller);
                var controllerDormant = !(swiped || wasSwiped || wasWasSwiped || touched || wasTouched || wasWasTouched);
                moved &= controllerDormant;
                pressed &= controllerDormant;

                wasWasSwiped = wasSwiped;
                wasWasTouched = wasTouched;
                wasSwiped = swiped;
                wasTouched = touched;
#endif
                return Application.isEditor
                    || system == XR.SystemTypes.UWP && platform != XR.PlatformTypes.UWPHoloLens
                    || system == XR.SystemTypes.Standalone
                    || system == XR.SystemTypes.WebGL
                    || moved
                    || pressed
                    || IsButtonPressed(KeyCode.Mouse1)
                    || IsButtonPressed(KeyCode.Mouse2)
                    || IsButtonPressed(KeyCode.Mouse3)
                    || IsButtonPressed(KeyCode.Mouse4)
                    || IsButtonPressed(KeyCode.Mouse5)
                    || IsButtonPressed(KeyCode.Mouse6);
            }
        }

        /// <summary>
        /// Disables gazing for the pointer.
        /// </summary>
        public override void OnProbeFound()
        {
            base.OnProbeFound();

            if (probe != null)
            {
                probe.CanGaze = false;
            }
        }

        /// <summary>
        /// The screen-space position of the mouse cursor.
        /// </summary>
        public override Vector3 WorldPoint
        {
            get
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    return WorldFromViewport(VIEWPORT_MIDPOINT);
                }
                else
                {
                    return WorldFromScreen(UnityInput.mousePosition);
                }
            }
        }

        /// <summary>
        /// Manages cursor lock, quits out of the application if the user hits Escape while not
        /// cursor locked, and makes the camera follow the pointer if the cursor is locked.
        /// </summary>
        protected override void InternalUpdate()
        {
            if (UnityInput.GetKey(KeyCode.LeftShift)
                || UnityInput.GetKey(KeyCode.RightShift))
            {
                ScrollDelta = new Vector2(UnityInput.mouseScrollDelta.y, 0);
            }
            else
            {
                ScrollDelta = UnityInput.mouseScrollDelta;
            }

            showProbe = !Cursor.visible;

            base.InternalUpdate();
        }

        public override bool IsButtonPressed(KeyCode button)
        {
            return UnityInput.GetKey(button);
        }

        public override bool IsButtonDown(KeyCode button)
        {
            return UnityInput.GetKeyDown(button);
        }

        public override bool IsButtonUp(KeyCode button)
        {
            return UnityInput.GetKeyUp(button);
        }

        protected override AbstractHapticDevice MakeHapticsDevice()
        {
            return this.Ensure<NoHaptics>();
        }
    }
}
