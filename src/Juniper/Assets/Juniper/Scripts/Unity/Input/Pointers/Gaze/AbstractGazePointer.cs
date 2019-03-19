using Juniper.Unity.Haptics;
using Juniper.Unity.Input.Pointers.Screen;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Juniper.Unity.Input.Pointers.Gaze
{
    public abstract class AbstractGazePointer<HapticsType> :
        AbstractScreenDevice<Unary, HapticsType, UnaryPointerConfiguration>
        where HapticsType : AbstractHapticDevice
    {
        protected bool gazed, wasGazed;

        private GameObject lastTarget;

        [Range(0, 5)]
        public float gazeThreshold = 2;

        private float gazeTime;

        public override bool IsConnected
        {
            get
            {
                return true;
            }
        }

        public override bool IsButtonPressed(Unary button)
        {
            return gazed;
        }

        public override bool IsButtonDown(Unary button)
        {
            return gazed && !wasGazed;
        }

        public override bool IsButtonUp(Unary button)
        {
            return !gazed && wasGazed;
        }

        /// <summary>
        /// Disables gazing for the pointer.
        /// </summary>
        public override void SetProbe(Probe p)
        {
            base.SetProbe(p);

            if (probe != null)
            {
                probe.canGaze = gazeThreshold > 0;
            }
        }

        private GameObject target;

        public override void Process(PointerEventData evtData, float pixelDragThresholdSquared)
        {
            target = evtData.pointerCurrentRaycast.gameObject;

            base.Process(evtData, pixelDragThresholdSquared);
        }

        protected override void InternalUpdate()
        {
            if (target != lastTarget)
            {
                gazeTime = Time.time;
            }
            lastTarget = target;

            wasGazed = gazed;

            if (target == null)
            {
                gazed = false;
                probe?.SetGaze(0);
            }
            else if (gazeThreshold > 0)
            {
                var deltaTime = Time.time - gazeTime;
                gazed = gazeThreshold <= deltaTime
                    && deltaTime < (gazeThreshold + 0.125f);
                probe.SetGaze(deltaTime / gazeThreshold);
            }
        }
    }
}
