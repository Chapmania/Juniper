#if WAVEVR

namespace Juniper.Input
{
    public abstract class ViveFocusInputModule : AbstractUnifiedInputModule
    {
        public override void Install(bool reset)
        {
            base.Install(reset);

            if (!reset && mode == Mode.Auto)
            {
                mode = Mode.StandingVR;
            }
        }

        public override bool HasFloorPosition { get { return true; } }
    }
}
#endif
