#if (UNITY_IOS || UNITY_ANDROID) && !GOOGLEVR && !UNITY_XR_OCULUS
#define HAS_HAPTICS
#endif

using System.Collections;

using UnityEngine;

#if UNITY_STANDALONE || UNITY_EDITOR

using XInputDotNetPure;

#elif UNITY_XR_WINDOWSMR_METRO
using System.Linq;
using Windows.Gaming.Input;
#endif

namespace Juniper.Unity.Haptics
{
    /// <summary>
    /// When no specific haptic implementation is available, but we know the system supports haptics,
    /// we fallback to using Unity's built-in Vibrate function, which is pretty primitive.
    /// </summary>
    public class DefaultHaptics : AbstractHapticExpressor
    {
#if UNITY_XR_WINDOWSMR_METRO && !UNITY_EDITOR
        Gamepad gp;
        public void Awake()
        {
            gp = Gamepad.Gamepads.FirstOrDefault();
            if (gp == null)
            {
                Gamepad.GamepadAdded += Gamepad_GamepadAdded;
                Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
            }
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            gp = e;
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            if (gp == e)
            {
                gp = null;
            }
        }

#endif

        /// <summary>
        /// Set a haptic pulse playing at a set amplitude.
        /// </summary>
        /// <param name="amplitude">The strength of the vibration.</param>
        private void SetVibration(float amplitude)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            GamePad.SetVibration(PlayerIndex.One, amplitude, amplitude);

#elif UNITY_XR_WINDOWSMR_METRO
            if (gp != null)
            {
                gp.Vibration = new GamepadVibration
                {
                    LeftMotor = amplitude,
                    RightMotor = amplitude
                };
            }
#endif
        }

        /// <summary>
        /// Cancel the current vibration, whatever it is.
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            SetVibration(0);
        }

        /// <summary>
        /// Play a single vibration of a set length of time.
        /// </summary>
        /// <param name="milliseconds">Milliseconds.</param>
        /// <param name="amplitude">   The strenght of vibration (ignored).</param>
        protected override IEnumerator VibrateCoroutine(long milliseconds, float amplitude)
        {
            var seconds = Units.Milliseconds.Seconds(milliseconds);
#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_XR_WINDOWSMR_METRO
            SetVibration(amplitude);
#elif HAS_HAPTICS
            if (amplitude > 0.25f)
            {
                var now = Time.time;
                while (Time.time - now < seconds)
                {
                    Handheld.Vibrate();
                    yield return null;
                }
            }
            else
            {
#endif
            yield return new WaitForSeconds(seconds);
#if !UNITY_STANDALONE && !UNITY_EDITOR && !UNITY_XR_WINDOWSMR_METRO && HAS_HAPTICS
            }
#endif
        }
    }
}