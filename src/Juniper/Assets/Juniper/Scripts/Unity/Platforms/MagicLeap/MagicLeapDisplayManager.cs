#if MAGIC_LEAP

using MSA;

using UnityEngine;

namespace Juniper.Display
{
    public class MagicLeapDisplayManager : AbstractDisplayManager
    {
        protected override float DEFAULT_FOV
        {
            get
            {
                return 30;
            }
        }

        public override void Install(bool reset)
        {
            reset &= Application.isEditor;

            base.Install(reset);

            listener.EnsureComponent<MSAListener>();
        }

        public override void Uninstall()
        {
            this.RemoveComponent<MSAListener>();

            base.Uninstall();
        }
    }
}

#endif