using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Juniper.IO;

using UnityEditor;

using UnityEngine;

using static System.Console;

namespace Juniper.ConfigurationManagement
{
    public static class Program
    {
        public static void Main()
        {
            //var unityProjectDir = @"C:\Users\smcbeth.DLS-INC\Projects\Yarrow\src\Yarrow - AndroidOculus";
            var unityProjectDir = @"D:\Projects\Juniper\examples\Juniper - Android";

            AbstractPackage.UnityProjectRoot = unityProjectDir;
            var packageDB = GetPackages();


            var configFactory = new JsonFactory<Platforms>();
            var juniperPath = Path.Combine(unityProjectDir, "Assets", "Juniper");
            var juniperPlatformsFileName = Path.Combine(juniperPath, "platforms.json");
            var platforms = configFactory.Deserialize(juniperPlatformsFileName);

            foreach (var package in platforms.Packages)
            {
                WriteLine(package);
            }
        }

        public static BuildTarget GetBuildTarget(PlatformConfiguration config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var localBuildTarget = config.BuildTarget;
            if (localBuildTarget == "Standalone")
            {
#if UNITY_EDITOR_WIN
                if (Environment.Is64BitOperatingSystem)
                {
                    localBuildTarget = nameof(UnityEditor.BuildTarget.StandaloneWindows64);
                }
                else
                {
                    localBuildTarget = nameof(UnityEditor.BuildTarget.StandaloneWindows);
                }
#elif UNITY_EDITOR_OSX
                localBuildTarget = nameof(UnityEditor.BuildTarget.StandaloneOSX);
#else
                localBuildTarget = nameof(UnityEditor.BuildTarget.StandaloneLinux64);
#endif
            }

            try
            {
                return (BuildTarget)Enum.Parse(typeof(BuildTarget), localBuildTarget);
            }
            catch
            {
                return UnityEditor.BuildTarget.NoTarget;
            }
        }

        public static BuildTargetGroup GetTargetGroup(PlatformConfiguration config)
        {
            return BuildPipeline.GetBuildTargetGroup(GetBuildTarget(config));
        }

        public static bool IsSupported(PlatformConfiguration config)
        {
            return BuildPipeline.IsBuildTargetSupported(GetTargetGroup(config), GetBuildTarget(config));
        }

        public static void SwitchTarget(PlatformConfiguration config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Debug.Log($"Switching build target from {EditorUserBuildSettings.activeBuildTarget} to {config.BuildTarget}.");
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(GetTargetGroup(config), GetBuildTarget(config));
        }

        public static bool TargetSwitchNeeded(PlatformConfiguration config)
        {
            return GetBuildTarget(config) != EditorUserBuildSettings.activeBuildTarget;
        }


        public static void Activate(PlatformConfiguration config, IProgress prog)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var progs = prog.Split(2);

            if (!string.IsNullOrEmpty(config.Spatializer))
            {
                var hasSpatializer = AudioSettings
                    .GetSpatializerPluginNames()
                    .Contains(config.Spatializer);

                if (hasSpatializer)
                {
                    AudioSettings.SetSpatializerPluginName(config.Spatializer);
                    AudioSettingsExt.SetAmbisonicDecoderPluginName(config.Spatializer);
                }
            }

            var buildTargetGroup = GetTargetGroup(config);
            var supportedVRSDKs = PlayerSettings.GetAvailableVirtualRealitySDKs(buildTargetGroup);
            var vrSDKs = config
                .XRPlatforms
                .Distinct()
                .Select(x => x.ToString())
                .Where(supportedVRSDKs.Contains)
                .ToArray();

            var enableVR = vrSDKs.Any(sdk => sdk != "None");
            if (enableVR != PlayerSettings.GetVirtualRealitySupported(buildTargetGroup))
            {
                PlayerSettings.SetVirtualRealitySupported(buildTargetGroup, enableVR);
                if (enableVR && !vrSDKs.Matches(PlayerSettings.GetVirtualRealitySDKs(buildTargetGroup)))
                {
                    PlayerSettings.SetVirtualRealitySDKs(buildTargetGroup, vrSDKs);
                }
            }

            if (buildTargetGroup == BuildTargetGroup.WSA)
            {
                EditorUserBuildSettings.wsaBuildAndRunDeployTarget = WSABuildAndRunDeployTarget.LocalMachine;
                EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;
                PlayerSettings.WSA.inputSource = PlayerSettings.WSAInputSource.IndependentInputSource;
                if (Enum.TryParse(config.WsaSubtarget, out WSASubtarget sub))
                {
                    EditorUserBuildSettings.wsaSubtarget = sub;
                    PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.SpatialPerception, sub == WSASubtarget.HoloLens);
                }
            }
            else if (buildTargetGroup == BuildTargetGroup.Android
                && Enum.TryParse(config.AndroidSdkVersion, out AndroidSdkVersions sdkVersion))
            {
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)Math.Max(
                    (int)PlayerSettings.Android.minSdkVersion,
                    (int)sdkVersion);
            }
            else if (buildTargetGroup == BuildTargetGroup.iOS
                && Version.TryParse(config.IOSVersion, out var v)
                && ApplleiOS.TargetOSVersion < v)
            {
                ApplleiOS.TargetOSVersion = v;
            }
        }

        public static void Activate(JuniperZipPackage package, BuildTargetGroup targetGroup)
        {
            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (package.Name == "Vuforia")
            {
                PlayerSettings.vuforiaEnabled = true;
            }

            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (targetGroup == BuildTargetGroup.Android)
            {
                if (package.Name == "GoogleARCore")
                {
                    PlayerSettings.Android.ARCoreEnabled = true;
                }
                else if (package.Name == "GoogleVR")
                {
                    FileExt.Copy(
                        PathExt.FixPath("Assets/GoogleVR/Plugins/Android/AndroidManifest-6DOF.xml"),
                        PathExt.FixPath("Assets/Plugins/Android/AndroidManifest.xml"),
                        true);
                }
            }
            else if (targetGroup == BuildTargetGroup.iOS)
            {
                if (package.Name == "UnityARKitPlugin")
                {
                    Juniper.ApplleiOS.RequiresARKitSupport = true;

                    if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
                    {
                        PlayerSettings.iOS.cameraUsageDescription = "Augmented reality camera view";
                    }
                }
            }
        }


        private static IReadOnlyDictionary<string, IReadOnlyCollection<AbstractPackage>> GetPackages()
        {
            var packages = new List<AbstractPackage>();

            UnityAssetStorePackage.GetPackages(packages);
            JuniperZipPackage.GetPackages(packages);
            UnityPackageManagerPackage.GetPackages(packages);

            return (from package in packages
                    group package by package.PackageID into grp
                    orderby grp.Key
                    select grp)
                    .ToDictionary(
                        g => g.Key,
                        g => (IReadOnlyCollection<AbstractPackage>)g.ToArray());
        }

        public static List<string> GetCompilerDefines(PlatformConfiguration config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var defines = new List<string>();

            if (!string.IsNullOrEmpty(config.CompilerDefine))
            {
                defines.MaybeAdd(config.CompilerDefine);
            }

            if (GetTargetGroup(config) == BuildTargetGroup.Android)
            {
                var target = PlayerSettings.Android.targetSdkVersion;
                if (target == AndroidSdkVersions.AndroidApiLevelAuto)
                {
                    target = Enum.GetValues(typeof(AndroidSdkVersions))
                        .Cast<AndroidSdkVersions>()
                        .Max();
                }
                for (var i = (int)target; i > 0; --i)
                {
                    if (Enum.IsDefined(typeof(AndroidSdkVersions), i))
                    {
                        defines.MaybeAdd("ANDROID_API_" + i);
                        defines.MaybeAdd("ANDROID_API_" + i + "_OR_GREATER");
                    }
                }
            }
            else if (GetTargetGroup(config) == BuildTargetGroup.iOS)
            {
                for (var i = ApplleiOS.TargetOSVersion.Major; i > 0; --i)
                {
                    defines.MaybeAdd("IOS_VERSION_" + i);
                    defines.MaybeAdd("IOS_VERSION_" + i + "_OR_GREATER");
                }
            }

            return defines
                .Distinct()
                .ToList();
        }

        public static void Deactivate()
        {
            AudioSettings.SetSpatializerPluginName(null);
            AudioSettingsExt.SetAmbisonicDecoderPluginName(null);
            PlayerSettings.virtualRealitySupported = false;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.ARCoreEnabled = false;
            ApplleiOS.RequiresARKitSupport = false;
            PlayerSettings.vuforiaEnabled = false;
        }
    }
}
