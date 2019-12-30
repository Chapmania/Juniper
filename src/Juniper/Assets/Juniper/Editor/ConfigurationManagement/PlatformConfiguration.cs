using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using Juniper.Progress;
using Juniper.XR;

using UnityEditor;

using UnityEngine;

namespace Juniper.ConfigurationManagement
{
    [Serializable]
    internal sealed class PlatformConfiguration : ISerializable
    {
        private static readonly string[] NO_VR_SYSTEMS = new string[] { "None" };

        public readonly string Name;
        public readonly string CompilerDefine;
        public readonly string buildTarget;
        public readonly string spatializer;
        public readonly string androidSdkVersion;
        public readonly string iOSVersion;
        public readonly string wsaSubtarget;
        public readonly string[] vrSystems;
        public readonly string[] packages;

        private PlatformConfiguration(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(nameof(Name));
            buildTarget = info.GetString(nameof(buildTarget));
            vrSystems = NO_VR_SYSTEMS;

            foreach (var field in info)
            {
                switch (field.Name)
                {
                    case nameof(CompilerDefine): CompilerDefine = info.GetString(nameof(CompilerDefine)); break;
                    case nameof(vrSystems): vrSystems = info.GetValue<string[]>(nameof(vrSystems)); break;
                    case nameof(packages): packages = info.GetValue<string[]>(nameof(packages)); break;
                    case nameof(spatializer): spatializer = info.GetString(nameof(spatializer)); break;
                    case nameof(androidSdkVersion): androidSdkVersion = info.GetString(nameof(androidSdkVersion)); break;
                    case nameof(iOSVersion): iOSVersion = info.GetString(nameof(iOSVersion)); break;
                    case nameof(wsaSubtarget): wsaSubtarget = info.GetString(nameof(wsaSubtarget)); break;
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(buildTarget), buildTarget);
            info.MaybeAddValue(nameof(CompilerDefine), CompilerDefine);
            info.MaybeAddValue(nameof(spatializer), spatializer);
            info.MaybeAddValue(nameof(androidSdkVersion), androidSdkVersion);
            info.MaybeAddValue(nameof(iOSVersion), iOSVersion);
            info.MaybeAddValue(nameof(wsaSubtarget), wsaSubtarget);
            info.MaybeAddValue(nameof(vrSystems), vrSystems);
            info.MaybeAddValue(nameof(packages), packages);
        }

        public BuildTarget BuildTarget
        {
            get
            {
                var localBuildTarget = buildTarget;
                if (localBuildTarget == "Standalone")
                {
#if UNITY_EDITOR_WIN
                    if (Environment.Is64BitOperatingSystem)
                    {
                        localBuildTarget = nameof(BuildTarget.StandaloneWindows64);
                    }
                    else
                    {
                        localBuildTarget = nameof(BuildTarget.StandaloneWindows);
                    }
#elif UNITY_EDITOR_OSX
                    localBuildTarget = nameof(BuildTarget.StandaloneOSX);
#else
                    if (Environment.Is64BitOperatingSystem)
                    {
                        localBuildTarget = nameof(BuildTarget.StandaloneLinux64);
                    }
                    else
                    {
                        localBuildTarget = nameof(BuildTarget.StandaloneLinux);
                    }
#endif
                }

                try
                {
                    return (BuildTarget)Enum.Parse(typeof(BuildTarget), localBuildTarget);
                }
                catch
                {
                    return BuildTarget.NoTarget;
                }
            }
        }

        public BuildTargetGroup TargetGroup
        {
            get
            {
                return BuildPipeline.GetBuildTargetGroup(BuildTarget);
            }
        }

        public bool IsSupported
        {
            get
            {
                return BuildPipeline.IsBuildTargetSupported(TargetGroup, BuildTarget);
            }
        }

        public IEnumerable<UnityXRPlatform> XRPlatforms
        {
            get
            {
                return from sys in vrSystems
                       select (UnityXRPlatform)Enum.Parse(typeof(UnityXRPlatform), sys);
            }
        }

        public UnityPackage[] UninstallableUnityPackages
        {
            get; internal set;
        }

        public UnityPackage[] IncludedUnityPackages
        {
            get; internal set;
        }

        public UnityPackage[] ExcludedUnityPackages
        {
            get; internal set;
        }

        public ZipPackage[] UninstallableZipPackages
        {
            get; internal set;
        }

        public ZipPackage[] ZipPackages
        {
            get; internal set;
        }

        public AssetStorePackage[] UninstallableAssetStorePackages
        {
            get; internal set;
        }

        public AssetStorePackage[] AssetStorePackages
        {
            get; internal set;
        }

        public void SwitchTarget()
        {
            Debug.Log($"Switching build target from {EditorUserBuildSettings.activeBuildTarget} to {BuildTarget}.");
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(TargetGroup, BuildTarget);
        }

        public bool TargetSwitchNeeded
        {
            get
            {
                return BuildTarget != EditorUserBuildSettings.activeBuildTarget;
            }
        }

        public void InstallUnityPackages(IProgress prog)
        {
            var progs = prog.Split(2);

            Platforms.ForEachPackage(ExcludedUnityPackages, progs[0], (pkg, p) =>
            {
                pkg.Uninstall(p);
            });

            Platforms.ForEachPackage(IncludedUnityPackages, progs[1], (pkg, p) =>
            {
                pkg.Install(p);
            });
        }

        public void InstallZipPackages(IProgress prog)
        {
            Platforms.ForEachPackage(ZipPackages, prog, (pkg, p) => pkg.Install(p));
        }

        public void InstallAssetStorePackages(IProgress prog)
        {
            Platforms.ForEachPackage(AssetStorePackages, prog, (pkg, p) => pkg.Install(p));
        }

        public void Activate(IProgress prog)
        {
            var progs = prog.Split(2);

            Platforms.ForEachPackage(
                IncludedUnityPackages, progs[0],
                (pkg, p) => pkg.Activate(TargetGroup, p));

            Platforms.ForEachPackage(
                ZipPackages, progs[1],
                (pkg, p) => pkg.Activate(TargetGroup, p));

            Platforms.ForEachPackage(
                AssetStorePackages, progs[1],
                (pkg, p) => pkg.Activate(TargetGroup, p));

            if (!string.IsNullOrEmpty(spatializer))
            {
                var hasSpatializer = AudioSettings
                    .GetSpatializerPluginNames()
                    .Contains(spatializer);

                if (hasSpatializer)
                {
                    AudioSettings.SetSpatializerPluginName(spatializer);
                    AudioSettingsExt.SetAmbisonicDecoderPluginName(spatializer);
                }
            }

            var supportedVRSDKs = PlayerSettings.GetAvailableVirtualRealitySDKs(TargetGroup);
            var vrSDKs = XRPlatforms
                .Distinct()
                .Select(x => x.ToString())
                .Where(supportedVRSDKs.Contains)
                .ToArray();

            var enableVR = vrSDKs.Any(sdk => sdk != "None");
            if (enableVR != PlayerSettings.GetVirtualRealitySupported(TargetGroup))
            {
                PlayerSettings.SetVirtualRealitySupported(TargetGroup, enableVR);
                if (enableVR && !vrSDKs.Matches(PlayerSettings.GetVirtualRealitySDKs(TargetGroup)))
                {
                    PlayerSettings.SetVirtualRealitySDKs(TargetGroup, vrSDKs);
                }
            }

            if (TargetGroup == BuildTargetGroup.WSA)
            {
                EditorUserBuildSettings.wsaBuildAndRunDeployTarget = WSABuildAndRunDeployTarget.LocalMachine;
                EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;
                PlayerSettings.WSA.inputSource = PlayerSettings.WSAInputSource.IndependentInputSource;
                WSASubtarget sub;
                if (Enum.TryParse(wsaSubtarget, out sub))
                {
                    EditorUserBuildSettings.wsaSubtarget = sub;
                    PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.SpatialPerception, sub == WSASubtarget.HoloLens);
                }
            }
            else if (TargetGroup == BuildTargetGroup.Android)
            {
                AndroidSdkVersions sdkVersion;
                if (Enum.TryParse(androidSdkVersion, out sdkVersion))
                {
                    PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)Math.Max(
                        (int)PlayerSettings.Android.minSdkVersion,
                        (int)sdkVersion);
                }
            }
            else if (TargetGroup == BuildTargetGroup.iOS)
            {
                Version v;
                if (Version.TryParse(iOSVersion, out v)
                    && Juniper.ApplleiOS.TargetOSVersion < v)
                {
                    Juniper.ApplleiOS.TargetOSVersion = v;
                }
            }
        }

        public void UninstallAssetStorePackages(IProgress prog)
        {
            Platforms.ForEachPackage(UninstallableAssetStorePackages, prog, (pkg, p) => pkg.Uninstall(p));
        }

        public void UninstallZipPackages(IProgress prog)
        {
            Platforms.ForEachPackage(UninstallableZipPackages, prog, (pkg, p) => pkg.Uninstall(p));
        }

        public void UninstallUnityPackages(IProgress prog)
        {
            Platforms.ForEachPackage(UninstallableUnityPackages, prog, (pkg, p) => pkg.Uninstall(p));
        }

        public List<string> CompilerDefines
        {
            get
            {
                var defines = (from pkg in IncludedUnityPackages
                               where pkg.version != "exclude"
                               select (AbstractPackage)pkg)
                    .Union(ZipPackages)
                    .Union(AssetStorePackages)
                    .Select(pkg => pkg.CompilerDefine)
                    .Where(def => !string.IsNullOrEmpty(def))
                    .ToList();

                if (!string.IsNullOrEmpty(CompilerDefine))
                {
                    defines.MaybeAdd(CompilerDefine);
                }

                if (TargetGroup == BuildTargetGroup.Android)
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
                else if (TargetGroup == BuildTargetGroup.iOS)
                {
                    for (var i = Juniper.ApplleiOS.TargetOSVersion.Major; i > 0; --i)
                    {
                        defines.MaybeAdd("IOS_VERSION_" + i);
                        defines.MaybeAdd("IOS_VERSION_" + i + "_OR_GREATER");
                    }
                }

                return defines
                    .Distinct()
                    .ToList();
            }
        }

        internal void Deactivate()
        {
            AudioSettings.SetSpatializerPluginName(null);
            AudioSettingsExt.SetAmbisonicDecoderPluginName(null);
            PlayerSettings.virtualRealitySupported = false;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.ARCoreEnabled = false;
            Juniper.ApplleiOS.RequiresARKitSupport = false;
            PlayerSettings.vuforiaEnabled = false;
        }
    }
}
