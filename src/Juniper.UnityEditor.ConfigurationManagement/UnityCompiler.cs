using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

namespace Juniper.ConfigurationManagement
{
    public static class UnityCompiler
    {
        private const string RECOMPILE_SLUG = "RECOMPILE_SLUG";

        public static List<string> CleanupDefines(IEnumerable<string> defs)
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

        public static List<string> GetDefines(BuildTargetGroup targetGroup)
        {
            var defString = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var defArray = defString.SplitX(';');
            var defines = CleanupDefines(defArray);
            defines.Remove(RECOMPILE_SLUG);
            return defines;
        }

        public static BuildTargetGroup CurrentBuildTargetGroup =>
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

        public static List<string> GetDefines()
        {
            return GetDefines(CurrentBuildTargetGroup);
        }

        public static void SetDefines(BuildTargetGroup targetGroup, List<string> nextDefines, bool forceRecompile)
        {
            nextDefines = UnityCompiler.CleanupDefines(nextDefines);
            var currentDefines = GetDefines(targetGroup);
            if (nextDefines.Matches(currentDefines) && forceRecompile)
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

            if (!nextDefines.Matches(currentDefines))
            {
                var defString = string.Join(";", nextDefines);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defString);
            }
        }

        public static void SetDefines(BuildTargetGroup targetGroup, List<string> nextDefines)
        {
            SetDefines(targetGroup, nextDefines, false);
        }

        public static void SetDefines(List<string> nextDefines, bool forceRecompile)
        {
            SetDefines(CurrentBuildTargetGroup, nextDefines, forceRecompile);
        }

        public static void SetDefines(List<string> nextDefines)
        {
            SetDefines(nextDefines, false);
        }
    }
}
