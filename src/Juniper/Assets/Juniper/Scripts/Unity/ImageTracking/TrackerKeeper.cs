namespace Juniper.Unity.ImageTracking
{
    public class TrackerKeeper :
#if ARCORE
        ARCoreTrackerKeeper
#elif ARKIT
        ARKitTrackerKeeper
#elif MAGIC_LEAP
        MagicLeapTrackerKeeper
#elif VUFORIA
        VuforiaTrackerKeeper
#else
        AbstractTrackerKeeper
#endif
    {
    }
}
