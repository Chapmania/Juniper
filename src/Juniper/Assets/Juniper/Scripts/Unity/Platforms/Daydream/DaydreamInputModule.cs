#if GOOGLEVR
using System.Linq;

using UnityEngine;

namespace Juniper.Unity.Input
{
    public abstract class DaydreamInputModule : AbstractUnifiedInputModule
    {
        public static bool AnyActiveGoogleInstantPreview =>
            ComponentExt.FindAll<Gvr.Internal.InstantPreview>()
                .Any(ComponentExt.IsActivated);

        protected override void Awake()
        {
            base.Awake();

            var gci = GetComponent<GvrControllerInput>();
            var gee = GetComponent<GvrEditorEmulator>();
            gci.enabled = gee.enabled = AnyActiveGoogleInstantPreview;
        }

        public override bool Install(bool reset)
        {
            reset &= Application.isEditor;

            var baseInstall = base.Install(reset);

            this.EnsureComponent<GvrControllerInput>();
            this.EnsureComponent<GvrEditorEmulator>();

#if UNITY_EDITOR
            var ip = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                System.IO.PathExt.FixPath("Assets/GoogleVR/Prefabs/InstantPreview/GvrInstantPreviewMain.prefab"));
            if(ip != null)
            {
                UnityEditor.PrefabUtility.InstantiatePrefab(ip);
            }
#endif

            return baseInstall;
        }

        public override void Uninstall()
        {
            base.Uninstall();

            this.RemoveComponent<GvrEditorEmulator>();
            this.RemoveComponent<GvrControllerInput>();
            var ip = ComponentExt.FindAny<InstantPreviewHelper>();
            if (ip != null)
            {
                ip.gameObject.Destroy();
            }
        }
    }
}
#endif