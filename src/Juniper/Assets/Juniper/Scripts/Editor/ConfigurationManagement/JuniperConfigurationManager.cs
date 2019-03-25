using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Juniper.Progress;
using Juniper.Unity;
using Juniper.Unity.Progress;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;

using UnityEngine;

namespace Juniper.UnityEditor.ConfigurationManagement
{
    /// <summary>
    /// An editor to respond to changes in XRSystem.
    /// </summary>
    [CustomEditor(typeof(JuniperPlatform))]
    public class JuniperConfigurationManager : Editor
    {
        private const string MENU_NAME = "Juniper/";

        private static readonly ProjectConfiguration config;

        static JuniperConfigurationManager()
        {
            config = ProjectConfiguration.Load();
            config.PlatformChanged += Config_PlatformChanged;
            config.PlatformChangeConfirmed += StartBuild;

            if (RebuildNeeded)
            {
                OnEditorUpdate(() =>
                    NextPlatform = DesiredPlatform,
                    () => EditorUtility.DisplayDialog("Juniper", "Could not change platform.", "OK"));
            }
        }

        private static int BuildProgress
        {
            get
            {
                return config.BuildStep;
            }

            set
            {
                config.BuildStep = value;
            }
        }

        private static PlatformTypes CurrentPlatform
        {
            get
            {
                return JuniperPlatform.CurrentPlatform;
            }
        }

        private static PlatformTypes DesiredPlatform
        {
            get
            {
                return config.CurrentPlatform;
            }
        }

        private static bool BuildInProgress
        {
            get
            {
                return 0 <= BuildProgress && BuildProgress < STAGES.Length;
            }
        }

        private static bool RebuildNeeded
        {
            get
            {
                return DesiredPlatform != CurrentPlatform && !BuildInProgress;
            }
        }

        private static PlatformTypes NextPlatform
        {
            get
            {
                return config.NextPlatform;
            }

            set
            {
                config.NextPlatform = value;
            }
        }

        private static bool Config_PlatformChanged()
        {
            if (!Platforms.Instance.PlatformDB[config.NextPlatform].IsSupported)
            {
                EditorUtility.DisplayDialog(
                   "Juniper",
                   $"The platform {config.NextPlatform} is not supported on your current system.",
                   "OK");
                return false;
            }
            else
            {
                return config.NextPlatform == PlatformTypes.None
                    || (config.NextPlatform != CurrentPlatform
                        && EditorUtility.DisplayDialog(
                            "Juniper",
                            $"You are about to change the platfrom from {CurrentPlatform} to {config.NextPlatform}. Do you want to continue?",
                            "Change platform",
                            "Don't change platform"));
            }
        }

        private static PlatformConfiguration LastConfiguration
        {
            get
            {
                return Platforms.Instance.PlatformDB.Get(CurrentPlatform);
            }
        }

        private static PlatformConfiguration NextConfiguration
        {
            get
            {
                return Platforms.Instance.PlatformDB.Get(NextPlatform);
            }
        }

        /// <summary>
        /// The DEFINE symbols that we use in this project. We need to filter them out of the whole
        /// set so we can add the right ones back in without deleting any that we don't control.
        /// </summary>
        private static string[] JUNIPER_DEFINES
        {
            get
            {
                var d = Platforms.Instance.AllCompilerDefines.ToList();
                d.Add(RECOMPILE_SLUG);
                return d.Distinct().ToArray();
            }
        }

        #region Menu

        private static bool MenuCheck(PlatformTypes p)
        {
            return CurrentPlatform != p && Platforms.Instance.PlatformDB[p].IsSupported;
        }

        #region Menu/Android

        private const string ANDROID_MENU_NAME = MENU_NAME + "Android/";

        [MenuItem(ANDROID_MENU_NAME + "None", true)]
        public static bool SetAndroid_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.Android);
        }

        [MenuItem(ANDROID_MENU_NAME + "None", false)]
        public static void SetAndroid_MenuItem()
        {
            NextPlatform = PlatformTypes.Android;
        }

        [MenuItem(ANDROID_MENU_NAME + "ARCore", true)]
        public static bool SetAndroid_ARCore_MenuItem_Validate()
        {
#if UNITY_2018_2_OR_NEWER
            return MenuCheck(PlatformTypes.AndroidARCore);
#else
            return false;
#endif
        }

        [MenuItem(ANDROID_MENU_NAME + "ARCore", false)]
        public static void SetAndroid_ARCore_MenuItem()
        {
            NextPlatform = PlatformTypes.AndroidARCore;
        }

        [MenuItem(ANDROID_MENU_NAME + "Cardboard", true)]
        public static bool SetAndroid_Cardboard_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.AndroidCardboard);
        }

        [MenuItem(ANDROID_MENU_NAME + "Cardboard", false)]
        public static void SetAndroid_Cardboard_MenuItem()
        {
            NextPlatform = PlatformTypes.AndroidCardboard;
        }

        [MenuItem(ANDROID_MENU_NAME + "Daydream", true)]
        public static bool SetAndroid_Daydream_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.AndroidDaydream);
        }

        [MenuItem(ANDROID_MENU_NAME + "Daydream", false)]
        public static void SetAndroid_Daydream_MenuItem()
        {
            NextPlatform = PlatformTypes.AndroidDaydream;
        }

        [MenuItem(ANDROID_MENU_NAME + "Oculus", true)]
        public static bool SetAndroid_Oculus_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.AndroidOculus);
        }

        [MenuItem(ANDROID_MENU_NAME + "Oculus", false)]
        public static void SetAndroid_Oculus_MenuItem()
        {
            NextPlatform = PlatformTypes.AndroidOculus;
        }

        [MenuItem(ANDROID_MENU_NAME + "Pico G2", true)]
        public static bool SetAndroid_PicoG2_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.AndroidPicoG2);
        }

        [MenuItem(ANDROID_MENU_NAME + "Pico G2", false)]
        public static void SetAndroid_PicoG2_MenuItem()
        {
            NextPlatform = PlatformTypes.AndroidPicoG2;
        }

        [MenuItem(ANDROID_MENU_NAME + "Vive Focus", true)]
        public static bool SetAndroid_ViveFocus_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.AndroidViveFocus);
        }

        [MenuItem(ANDROID_MENU_NAME + "Vive Focus", false)]
        public static void SetAndroid_ViveFocus_MenuItem()
        {
            NextPlatform = PlatformTypes.AndroidViveFocus;
        }

        #endregion Menu/Android

        #region Menu/IOS

        private const string IOS_MENU_NAME = MENU_NAME + "IOS/";

        [MenuItem(IOS_MENU_NAME + "None", true)]
        public static bool SetIOS_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.IOS);
        }

        [MenuItem(IOS_MENU_NAME + "None", false)]
        public static void SetIOS_MenuItem()
        {
            NextPlatform = PlatformTypes.IOS;
        }

        [MenuItem(IOS_MENU_NAME + "ARKit", true)]
        public static bool SetIOS_ARKit_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.IOSARKit);
        }

        [MenuItem(IOS_MENU_NAME + "ARKit", false)]
        public static void SetIOS_ARKit_MenuItem()
        {
            NextPlatform = PlatformTypes.IOSARKit;
        }

        [MenuItem(IOS_MENU_NAME + "Cardboard", true)]
        public static bool SetIOS_Cardboard_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.IOSCardboard);
        }

        [MenuItem(IOS_MENU_NAME + "Cardboard", false)]
        public static void SetIOS_Cardboard_MenuItem()
        {
            NextPlatform = PlatformTypes.IOSCardboard;
        }

        #endregion Menu/IOS

        #region Menu/Standalone

        private const string STANDALONE_MENU_NAME = MENU_NAME + "Standalone/";

        [MenuItem(STANDALONE_MENU_NAME + "None", true)]
        public static bool SetStandalone_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.Standalone);
        }

        [MenuItem(STANDALONE_MENU_NAME + "None", false)]
        public static void SetStandalone_MenuItem()
        {
            NextPlatform = PlatformTypes.Standalone;
        }

        [MenuItem(STANDALONE_MENU_NAME + "Oculus", true)]
        public static bool SetStandalone_Oculus_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.StandaloneOculus);
        }

        [MenuItem(STANDALONE_MENU_NAME + "Oculus", false)]
        public static void SetStandalone_Oculus_MenuItem()
        {
            NextPlatform = PlatformTypes.StandaloneOculus;
        }

        [MenuItem(STANDALONE_MENU_NAME + "SteamVR", true)]
        public static bool SetStandalone_SteamVR_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.StandaloneSteamVR);
        }

        [MenuItem(STANDALONE_MENU_NAME + "SteamVR", false)]
        public static void SetStandalone_SteamVR_MenuItem()
        {
            NextPlatform = PlatformTypes.StandaloneSteamVR;
        }

        #endregion Menu/Standalone

        #region Menu/UWP

        private const string UWP_MENU_NAME = MENU_NAME + "UWP/";

        [MenuItem(UWP_MENU_NAME + "None", true)]
        public static bool SetUWP_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.UWP);
        }

        [MenuItem(UWP_MENU_NAME + "None", false)]
        public static void SetUWP_MenuItem()
        {
            NextPlatform = PlatformTypes.UWP;
        }

        [MenuItem(UWP_MENU_NAME + "WindowsMR", true)]
        public static bool SetUWP_WindowsMR_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.UWPWindowsMR);
        }

        [MenuItem(UWP_MENU_NAME + "WindowsMR", false)]
        public static void SetUWP_WindowsMR_MenuItem()
        {
            NextPlatform = PlatformTypes.UWPWindowsMR;
        }

        [MenuItem(UWP_MENU_NAME + "HoloLens", true)]
        public static bool SetUWP_HoloLens_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.UWPHoloLens);
        }

        [MenuItem(UWP_MENU_NAME + "HoloLens", false)]
        public static void SetUWP_HoloLens_MenuItem()
        {
            NextPlatform = PlatformTypes.UWPHoloLens;
        }

        #endregion Menu/UWP

        #region Menu/LuminOS

        [MenuItem(MENU_NAME + "LuminOS/Magic Leap", true)]
        public static bool SetMagicLeap_MenuItem_Validate()
        {
            return MenuCheck(PlatformTypes.MagicLeap);
        }

        [MenuItem(MENU_NAME + "LuminOS/Magic Leap", false)]
        public static void SetMagicLeap_MenuItem()
        {
            NextPlatform = PlatformTypes.MagicLeap;
        }

        #endregion Menu/LuminOS

        #region Menu/Other

        private const string OTHER_MENU_NAME = MENU_NAME + "Other/";

        [MenuItem(OTHER_MENU_NAME + "Clear Errant Progress Dialogs", false, 202)]
        public static void ClearProgressDialog_MenuItem()
        {
            EditorUtility.ClearProgressBar();
        }

        [MenuItem(OTHER_MENU_NAME + "Reset configuration", false, 203)]
        public static void Other_ResetConfiguration()
        {
            StartBuild();
        }

        [MenuItem(OTHER_MENU_NAME + "Resume build", true)]
        public static bool Other_ResumeBuild_Validate()
        {
            return BuildInProgress;
        }

        [MenuItem(OTHER_MENU_NAME + "Resume build", false, 204)]
        public static void Other_ResumeBuild()
        {
            ResumeBuild();
        }

        #endregion Menu/Other

        #endregion Menu

        private const string RECOMPILE_SLUG = "RECOMPILE_SLUG";

        private static void OnCancel()
        {
            OnCancel(true);
        }

        private static void OnCancel(bool canceledByUser)
        {
            if (canceledByUser)
            {
                Debug.LogWarning("Canceled by user");
            }
            EditorUtility.ClearProgressBar();
            BuildProgress = -1;
        }

        private static void OnEditorUpdate(Action resolve, Action reject = null, Func<bool> test = null)
        {
            if (test == null)
            {
                test = () => true;
            }

            EditorApplication.CallbackFunction exec = null;
            exec = () =>
            {
                if (test())
                {
                    try
                    {
                        EditorApplication.update -= exec;
                        resolve();
                    }
                    catch (OperationCanceledException)
                    {
                        EditorApplication.update -= exec;
                        reject();
                    }
                }
            };

            EditorApplication.update += exec;
        }

        private static void DelayedUpdate(IProgress prog, float maxTime, Action onComplete)
        {
            prog?.Report(0);
            var startTime = DateTime.Now;
            OnEditorUpdate(() =>
            {
                EditorUtility.ClearProgressBar();
                onComplete();
            }, OnCancel, () =>
            {
                var age = (float)(DateTime.Now - startTime).TotalSeconds;
                prog?.Report(age, maxTime);
                return age > maxTime;
            });
        }

        private static List<string> CleanupDefines(IEnumerable<string> defs)
        {
            var defines = defs.Distinct().ToList();
            defines.RemoveAll(string.IsNullOrWhiteSpace);
            defines.Sort();

            // move the slug to the end, if it exists
            if (defines.Contains(RECOMPILE_SLUG))
            {
                defines.Remove(RECOMPILE_SLUG);
                defines.Add(RECOMPILE_SLUG);
            }

            return defines;
        }

        private static string LastPrefix;

        private static IProgress PrepareBuildStep(int offset, string prefix = null)
        {
            LastPrefix = prefix ?? LastPrefix;
            var step = BuildProgress - offset + 1;
            var msg = $"Juniper ({step} of {STAGES.Length}): {LastPrefix}";
            return new UnityEditorProgressDialog(msg)
                .Subdivide(step + BuildProgress, 2 * STAGES.Length, LastPrefix);
        }

        private static void WithProgress(string prefix, Action<IProgress> act)
        {
            try
            {
                var prog = PrepareBuildStep(0, prefix);
                prog.Report(0);
                act(prog);
            }
            catch (OperationCanceledException)
            {
                OnCancel();
            }
            catch (Exception exp)
            {
                Debug.LogException(exp);
            }
        }

        private static void Recompile(bool advanceBuildStep, params PlatformConfiguration[] platforms)
        {
            var prog = PrepareBuildStep(1);

            InternalCompile(advanceBuildStep, platforms);

            DelayedUpdate(prog, 30, () => Debug.LogWarning("Timeout!!!"));
        }

        private static void InternalCompile(bool advanceBuildStep, params PlatformConfiguration[] platforms)
        {
            if (advanceBuildStep)
            {
                ++BuildProgress;
            }

            var commonDefines = platforms
                .SelectMany(p => p.CompilerDefines)
                .GroupBy(d => d)
                .Where(defs => defs.Count() == platforms.Length)
                .SelectMany(defs => defs)
                .Distinct()
                .ToList();

            var currentDefines = CleanupDefines(PlayerSettings
                .GetScriptingDefineSymbolsForGroup(platforms[0].TargetGroup)
                .Split(';'))
                .ToList();

            var nonJuniper = currentDefines
                .Exclude(JUNIPER_DEFINES)
                .Where(def => !def.StartsWith("NO_UNITY"))
                .Distinct()
                .ToList();

            var nextDefines = nonJuniper
                .Union(commonDefines)
                .ToList();

            nextDefines = CleanupDefines(nextDefines);

            if (nextDefines.Matches(currentDefines))
            {
                if (nextDefines.Contains(RECOMPILE_SLUG))
                {
                    nextDefines.Remove(RECOMPILE_SLUG);
                }
                else
                {
                    nextDefines.Add(RECOMPILE_SLUG);
                }
            }

            var nextDefinesString = string.Join(";", nextDefines);

            var targetGroups = platforms
                .Select(p => p.TargetGroup)
                .Distinct()
                .Reverse()
                .ToArray();

            foreach(var targetGroup in targetGroups)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, nextDefinesString);
            }
        }

        private static readonly Action[] STAGES =
        {
            DeactivatePlatform,
            RefreshPackages,
            PrepareProject
        };

        [DidReloadScripts]
        private static void ResumeBuild()
        {
            if (BuildInProgress)
            {
                STAGES[BuildProgress].Invoke();
            }
        }

        private static readonly string MANIFEST_FILE = PathExt.FixPath(
#if UNITY_2018_1_OR_NEWER
            "Packages/manifest.json"
#else
            "UnityPackageManager/manifest.json"
#endif
        );

        private static void StartBuild()
        {
            BuildProgress = 0;
            DeactivatePlatform();
        }

        private static void DeactivatePlatform()
        {
            WithProgress("Resetting to base configuration", _ =>
            {
                JuniperPlatform.Uninstall();
                LastConfiguration.Deactivate(NextConfiguration);
                Recompile(true, LastConfiguration, NextConfiguration);
            });
        }

        public static void RefreshPackages()
        {
            WithProgress("Refreshing packages " + NextPlatform, prog =>
            {
                var progs = prog.Split(4);
                LastConfiguration.UninstallRawPackages(progs[0]);

                var manifest = JObject.Parse(File.ReadAllText(MANIFEST_FILE));
                var deps = manifest["dependencies"] as JObject;
                if (deps == null)
                {
                    deps = new JObject();
                    manifest.Add("dependencies", deps);
                }

                UnityPackage.Dependencies = deps;
                LastConfiguration.UninstallUnityPackages(progs[1]);
                NextConfiguration.InstallUnityPackages(progs[2]);

                var txt = manifest.ToString(Formatting.Indented);
                FileExt.WriteAllText(MANIFEST_FILE, txt);

                NextConfiguration.InstallRawPackages(progs[3]);

                if (NextConfiguration.TargetSwitchNeeded)
                {
                    InternalCompile(false, NextConfiguration);
                    NextConfiguration.SwitchTarget();
                }
                else
                {
                    InternalCompile(true, NextConfiguration);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                    if (!EditorApplication.isCompiling)
                    {
                        ResumeBuild();
                    }
                }
            });
        }

        public static void PrepareProject()
        {
            WithProgress("Preparing project", prog =>
            {
                NextConfiguration.Activate(prog);
                JuniperPlatform.Install(true);
                config.Commit();
                OnCancel(false);
            });
        }
    }
}
