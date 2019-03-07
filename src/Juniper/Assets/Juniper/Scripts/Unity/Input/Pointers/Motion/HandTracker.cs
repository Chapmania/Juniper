using Juniper.Input;

using System;

using UnityEngine;

namespace Juniper.Unity.Input.Pointers.Motion
{
    public class HandTracker :
#if HOLOLENS
        HoloLensHand
#elif UNITY_XR_MAGICLEAP
        MagicLeapHand
#elif LEAP_MOTION
        LeapMotionHand
#else
        NoHandTracker
#endif
    {
        [ContextMenu("Reinstall")]
        public override void Reinstall()
        {
            base.Reinstall();
        }

        public static HandTracker[] MakeHandTrackers(Func<string, HandTracker> MakePointer)
        {
            return new[] {
                MakeHandTracker(MakePointer, Hands.Left),
                MakeHandTracker(MakePointer, Hands.Right)
            };
        }

        /// <summary>
        /// Create a new hand pointer object for an interaction source that hasn't yet been seen.
        /// </summary>
        private static HandTracker MakeHandTracker(Func<string, HandTracker> MakePointer, Hands hand)
        {
            var pointer = MakePointer(PointerConfig.MakePointerName(hand));
            pointer.Hand = hand;
            return pointer;
        }
    }
}
