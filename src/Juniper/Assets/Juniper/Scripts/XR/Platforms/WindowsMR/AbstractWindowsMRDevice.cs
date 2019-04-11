#if UNITY_XR_WINDOWSMR_METRO
using Juniper.Unity.Haptics;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace Juniper.Unity.Input.Pointers.Motion
{
    public enum WindowsMRButtons
    {
        AirTap,
        Any,
        App,
        Select,
        Grip,
        Thumbstick,
        Touchpad
    }

    /// <summary>
    /// A motion controller or hand-tracking. Currently only implements WindowsMR.
    /// </summary>
    public abstract class AbstractWindowsMRDevice<ConfigType, HapticsType>
        : AbstractHandedPointer<InteractionSourceHandedness, WindowsMRButtons, ConfigType, HapticsType>
        where ConfigType : AbstractHandedPointerConfiguration<InteractionSourceHandedness, WindowsMRButtons>, new()
        where HapticsType : AbstractHapticDevice
    {
        private static InteractionSourceState[] states;

        public static void UpdateReadings()
        {
            if (states == null || InteractionManager.numSourceStates != states.Length)
            {
                states = new InteractionSourceState[InteractionManager.numSourceStates];
            }

            InteractionManager.GetCurrentReading(states);
        }

        private static readonly Dictionary<WindowsMRButtons, Func<InteractionSourceState, bool>> BUTTONS = new Dictionary<WindowsMRButtons, Func<InteractionSourceState, bool>>(7)
        {
            { WindowsMRButtons.AirTap, s => s.selectPressed },
            { WindowsMRButtons.App, s => s.menuPressed },
            { WindowsMRButtons.Any, s => s.anyPressed },
            { WindowsMRButtons.Grip, s => s.grasped },
            { WindowsMRButtons.Select, s => s.selectPressed },
            { WindowsMRButtons.Thumbstick, s => s.thumbstickPressed },
            { WindowsMRButtons.Touchpad, s => s.touchpadPressed }
        };

        public override bool IsConnected
        {
            get
            {
                return states?.Any(obj => obj.source.handedness == NativeHandID) == true;
            }
        }

        /// <summary>
        /// The time at which the last State frame came in.
        /// </summary>
        private float updateTime;

        /// <summary>
        /// If there hasn't been a State frame update in over a second, then we will consider the
        /// hand as "lost" and drop it from pointer events.
        /// </summary>
        /// <value><c>true</c> if is dead; otherwise, <c>false</c>.</value>
        public bool IsDead { get { return Time.unscaledTime - updateTime > 1; } }

        public override bool IsDominantHand { get { return IsRightHand; } }

        /// <summary>
        /// The most recent controller State frame.
        /// </summary>
        private InteractionSourceState _deviceState;
        protected InteractionSourceState lastInputState;

        /// <summary>
        /// Gets and sets the most recent controller State frame. On set, also decomposes the state
        /// frame into constituent parts that <see cref="UnifiedInputModule"/> can use to fire events.
        /// </summary>
        /// <value>The state of the input.</value>
        public virtual InteractionSourceState InputState
        {
            get
            {
                return _deviceState;
            }
            set
            {
                _deviceState = value;
                updateTime = Time.unscaledTime;
            }
        }

        protected override void InternalUpdate()
        {
            lastInputState = InputState;

            if (states != null)
            {
                foreach (var state in states)
                {
                    if (state.source.handedness == NativeHandID)
                    {
                        InputState = state;
                        break;
                    }
                }
            }

            var pose = InputState.sourcePose;

            Vector3 point;
            if (pose.TryGetPosition(out point, InteractionSourceNode.Pointer))
            {
                transform.localPosition = point;
            }

            Quaternion rot;
            if (pose.TryGetRotation(out rot, InteractionSourceNode.Pointer))
            {
                transform.localRotation = rot;
            }

            base.InternalUpdate();
        }

        public override bool IsButtonPressed(WindowsMRButtons button)
        {
            return BUTTONS[button](InputState);
        }

        public override bool IsButtonDown(WindowsMRButtons button)
        {
            return BUTTONS[button](InputState)
                && !BUTTONS[button](lastInputState);
        }

        public override bool IsButtonUp(WindowsMRButtons button)
        {
            return !BUTTONS[button](InputState)
                && BUTTONS[button](lastInputState);
        }
    }
}
#endif
