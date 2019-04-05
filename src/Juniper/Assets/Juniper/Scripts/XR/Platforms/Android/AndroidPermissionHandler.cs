#if UNITY_ANDROID && ANDROID_API_23_OR_GREATER
using UnityEngine.Android;

namespace Juniper.Unity.Permissions
{
    public abstract class AndroidPermissionHandler : AbstractPermissionHandler
    {
        public void Start()
        {
#if UNITY_2018_1_OR_NEWER
            foreach (var permission in new string[]
            {
                Permission.Microphone
            })
            {
                if (!Permission.HasUserAuthorizedPermission(permission))
                {
                    Permission.RequestUserPermission(permission);

                    if (Permission.HasUserAuthorizedPermission(permission))
                    {
                        Debug.Log($"The user granted permission {permission}");
                    }
                    else
                    {
                        Debug.Log($"The user DID NOT grant permission {permission}");
                    }
                }
            }
#endif
        }
    }
}
#endif
