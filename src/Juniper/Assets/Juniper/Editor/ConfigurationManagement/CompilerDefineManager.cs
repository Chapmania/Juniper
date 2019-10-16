using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Juniper.XR;

using UnityEditor;

using UnityEngine;

namespace Juniper.ConfigurationManagement
{
    /// <summary>
    /// An editor to respond to changes in XRSystem.
    /// </summary>
    [SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>")]
    [SuppressMessage("Performance", "HAA0102:Non-overridden virtual method call on value type", Justification = "<Pending>")]
    [SuppressMessage("Performance", "HAA0202:Value type to reference type conversion allocation for string concatenation", Justification = "<Pending>")]
    [SuppressMessage("Performance", "HAA0301:Closure Allocation Source", Justification = "<Pending>")]
    [SuppressMessage("Performance", "HAA0302:Display class allocation to capture closure", Justification = "<Pending>")]
    [SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "<Pending>")]
    [SuppressMessage("Performance", "HAA0603:Delegate allocation from a method group", Justification = "<Pending>")]
    public class CompilerDefineManager : EditorWindow
    {
        private static readonly GUIContent TITLE = new GUIContent("Compiler Defines");
        private const string MENU_NAME = "Juniper/";

        [MenuItem(MENU_NAME + "Compiler Defines Manager")]
        public static void ShowJuniperWindow()
        {
            EditorWindow.GetWindow<CompilerDefineManager>();
        }

        private static readonly ProjectConfiguration config = ProjectConfiguration.Load();

        private static string newDefine;

        private const float nameFieldWidth = 200;
        private const float narrowWidth = 50;
        private const float buttonWidth = 100;

        private static readonly GUILayoutOption nameFieldGWidth = GUILayout.Width(nameFieldWidth);
        private static readonly GUILayoutOption narrowGWidth = GUILayout.Width(narrowWidth);
        private static readonly GUILayoutOption buttonGWidth = GUILayout.Width(buttonWidth);

        private static readonly TableView definesTable = new TableView(
            "Defines",
            ("Define", nameFieldWidth),
            ("Required", buttonWidth)
        );


        public void OnGUI()
        {
            titleContent = TITLE;

            var nextDefines = UnityCompiler.GetDefines(CurrentConfiguration.TargetGroup);

            if (GUILayout.Button("Refresh"))
            {
                nextDefines = UnityCompiler.CleanupDefines(CurrentConfiguration.CompilerDefines);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(CurrentConfiguration.TargetGroup, string.Join(";", nextDefines));
            }

            using (_ = definesTable.Begin())
            {
                using (_ = new HGroup())
                {
                    newDefine = EditorGUILayout.TextField(newDefine, GUILayout.Width(nameFieldWidth + narrowWidth));
                    if (GUILayout.Button("Add", buttonGWidth))
                    {
                        if (!string.IsNullOrEmpty(newDefine))
                        {
                            nextDefines.Add(newDefine);
                        }
                        newDefine = string.Empty;
                    }
                }

                for (var i = 0; i < nextDefines.Count; ++i)
                {
                    var define = nextDefines[i];
                    using (_ = new HGroup())
                    {
                        EditorGUILayout.LabelField(new GUIContent(define, define), nameFieldGWidth);

                        EditorGUILayout.LabelField(
                            DesiredConfiguration.CompilerDefines.Contains(define).ToYesNo(),
                            EditorStyles.centeredGreyMiniLabel,
                            narrowGWidth);

                        if (GUILayout.Button("Remove", buttonGWidth))
                        {
                            nextDefines.RemoveAt(i);
                            --i;
                        }
                    }
                }
            }

            UnityCompiler.SetDefines(CurrentConfiguration.TargetGroup, nextDefines);
        }

        private static PlatformTypes DesiredPlatform
        {
            get
            {
                return config.CurrentPlatform;
            }
        }

        private static PlatformConfiguration DesiredConfiguration
        {
            get
            {
                return Platforms.PlatformDB.Get(DesiredPlatform);
            }
        }

        private static PlatformTypes CurrentPlatform
        {
            get
            {
                return JuniperSystem.CurrentPlatform;
            }
        }

        private static PlatformConfiguration CurrentConfiguration
        {
            get
            {
                return Platforms.PlatformDB.Get(CurrentPlatform);
            }
        }
    }
}
