using Juniper.Unity.Input.Pointers;

using System;
using System.Collections.Generic;

using UnityEngine;

using UnityInput = UnityEngine.Input;

namespace Juniper.Unity.Input
{
    [RequireComponent(typeof(Camera))]
    public class CameraControl : MonoBehaviour
    {
        private static Quaternion NEUTRAL_POSITION_RESET = Quaternion.Euler(90f, 0f, 0f);
        private static Quaternion FLIP_IMAGE = Quaternion.Euler(0f, 0f, 180f);

        public enum Mode
        {
            None,
            Auto,
            Mouse,
            Gamepad,
            Touch,
            MagicWindow
        }

        public Mode mode = Mode.Auto;

        /// <summary>
        /// If we are running on a desktop system, set this value to true to lock the mouse cursor to
        /// the application window.
        /// </summary>
        public bool setMouseLock = true;

        public enum MouseButton
        {
            Left,
            Right,
            Middle,
            None = ~0
        }

        public MouseButton requiredMouseButton = MouseButton.None;
        public int requiredTouchCount = 1;
        public float dragThreshold = 10;

        public bool disableHorizontal;
        public bool disableVertical;

        /// <summary>
        /// The mouse is not as sensitive as the motion controllers, so we have to bump up the
        /// sensitivity quite a bit.
        /// </summary>
        private const float MOUSE_SENSITIVITY_SCALE = 50;

        /// <summary>
        /// The mouse is not as sensitive as the motion controllers, so we have to bump up the
        /// sensitivity quite a bit.
        /// </summary>
        private const float TOUCH_SENSITIVITY_SCALE = 1;

        /// <summary>
        /// How quickly the mouse moves horizontally
        /// </summary>
        [Range(0, 1)]
        public float sensitivityX = 0.5f;

        /// <summary>
        /// How quickly the mouse moves vertically
        /// </summary>
        [Range(0, 1)]
        public float sensitivityY = 0.5f;

        /// <summary>
        /// Minimum vertical value
        /// </summary>
        public float minimumY = -45F;

        /// <summary>
        /// Maximum vertical value
        /// </summary>
        public float maximumY = 85F;

        private Quaternion lastGyro = Quaternion.identity;

        private StageExtensions stage;

        private readonly Dictionary<Mode, bool> dragged = new Dictionary<Mode, bool>();
        private readonly Dictionary<Mode, bool> wasGestureSatisfied = new Dictionary<Mode, bool>();
        private readonly Dictionary<Mode, float> dragDistance = new Dictionary<Mode, float>();

        private UnifiedInputModule input;

        public void Awake()
        {
            stage = ComponentExt.FindAny<StageExtensions>();

            foreach (var mode in Enum.GetValues(typeof(Mode)))
            {
                wasGestureSatisfied[(Mode)mode] = false;
            }
        }

        public void Start()
        {
            input = ComponentExt.FindAny<UnifiedInputModule>();

            if (setMouseLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private bool GestureSatisfied(Mode mode)
        {
            if (mode == Mode.None)
            {
                return false;
            }
            else if (mode == Mode.Gamepad || mode == Mode.MagicWindow)
            {
                return true;
            }
            else if (mode == Mode.Touch)
            {
                if (UnityInput.touchCount != requiredTouchCount)
                {
                    return false;
                }
                else
                {
                    var touchPhase = UnityInput.GetTouch(requiredTouchCount - 1).phase;
                    return touchPhase == TouchPhase.Moved
                        || touchPhase == TouchPhase.Stationary;
                }
            }
            else
            {
                var btn = (int)requiredMouseButton;
                var pressed = requiredMouseButton == MouseButton.None || UnityInput.GetMouseButton(btn);
                var down = requiredMouseButton != MouseButton.None && UnityInput.GetMouseButtonDown(btn);
                return pressed && !down && (!setMouseLock || Cursor.lockState == CursorLockMode.Locked);
            }
        }

        private Vector3 PointerMovement(Mode mode)
        {
            switch (mode)
            {
                case Mode.Mouse:
                case Mode.Gamepad:
                return AxialMovement;

                case Mode.Touch:
                return MeanTouchPointMovement;

                default:
                return Vector3.zero;
            }
        }

        private Vector3 AxialMovement
        {
            get
            {
                return MOUSE_SENSITIVITY_SCALE * new Vector3(
                    -UnityInput.GetAxis("Mouse Y"),
                    UnityInput.GetAxis("Mouse X"));
            }
        }

        private Vector3 MeanTouchPointMovement
        {
            get
            {
                var delta = Vector2.zero;
                for (var i = 0; i < UnityInput.touchCount; ++i)
                {
                    delta += UnityInput.GetTouch(i).deltaPosition / UnityInput.touchCount;
                }
                delta = new Vector2(delta.y, -delta.x);
                return TOUCH_SENSITIVITY_SCALE * delta;
            }
        }

        private Quaternion OrientationDelta(Mode mode, bool disableVertical)
        {
            if (mode == Mode.MagicWindow)
            {
                var endQuat = NEUTRAL_POSITION_RESET
                        * UnityInput.gyro.attitude
                        * FLIP_IMAGE;
                var dRot = Quaternion.Inverse(lastGyro) * endQuat;
                lastGyro = endQuat;
                return dRot;
            }
            else
            {
                var move = PointerMovement(mode);
                if (disableVertical)
                {
                    move.x = 0;
                }
                else
                {
                    move.x *= sensitivityX;
                }

                if (disableHorizontal)
                {
                    move.y = 0;
                }
                else
                {
                    move.y *= sensitivityY;
                }

                move.z = 0;

                return Quaternion.Euler(move);
            }
        }

        private bool DragRequired(Mode mode)
        {
            return mode == Mode.Touch
                || (mode == Mode.Mouse
                    && requiredMouseButton != MouseButton.None);
        }

        private bool DragSatisfied(Mode mode)
        {
            if (!DragRequired(mode))
            {
                return true;
            }
            else
            {
                var move = PointerMovement(mode);
                if (DragRequired(mode) && !dragged.Get(mode, false))
                {
                    dragDistance[mode] = dragDistance.Get(mode, 0) + (move.magnitude / Screen.dpi);
                    dragged[mode] = Units.Inches.Millimeters(dragDistance[mode]) > dragThreshold;
                }
                return dragged[mode];
            }
        }

        private void CheckMouseLock()
        {
            if (setMouseLock)
            {
                if (UnityInput.mousePresent && UnityInput.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
#if UNITY_2018_1_OR_NEWER
                else if (UnityInput.GetKeyDown(KeyCode.Escape))
                {
                    Cursor.lockState = CursorLockMode.None;
                }
#endif

                Cursor.visible = Cursor.lockState == CursorLockMode.None
                    || Cursor.lockState == CursorLockMode.Confined;
            }
            else if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void Update()
        {
            CheckMouseLock();
            if (!input.AnyPointerDragging)
            {
                CheckMode(mode, disableVertical);
                if (mode == Mode.MagicWindow)
                {
                    CheckMode(Mode.Touch, true);
                }
                else if (mode == Mode.Gamepad)
                {
                    if (Application.isMobilePlatform)
                    {
                        CheckMode(Mode.Touch, disableVertical);
                    }
                    else
                    {
                        CheckMode(Mode.Mouse, disableVertical);
                    }
                }
            }
        }

        private void CheckMode(Mode mode, bool disableVertical)
        {
            ScreenDebugger.Print($"Checking mode {mode}");
            var gest = GestureSatisfied(mode);
            var wasGest = wasGestureSatisfied[mode];
            if (gest)
            {
                if (!wasGest)
                {
                    dragged[mode] = false;
                    dragDistance[mode] = 0;
                }

                if (DragSatisfied(mode))
                {
                    var delta = OrientationDelta(mode, disableVertical);
                    ScreenDebugger.Print($"{delta.Label()}");
                    stage.RotateView(delta, minimumY, maximumY);
                }
            }

            wasGestureSatisfied[mode] = gest;
        }
    }
}
