using System;
using System.Runtime.Serialization;

namespace Juniper.ConfigurationManagement
{
    [Serializable]
    public sealed class Platforms : ISerializable
    {
        public string[] Packages { get; }

        public PlatformConfiguration[] Configurations { get; }

        private Platforms(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Packages = info.GetValue<string[]>(nameof(Packages));
            Configurations = info.GetValue<PlatformConfiguration[]>(nameof(Configurations));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(Packages), Packages);
            info.AddValue(nameof(Configurations), Configurations);
        }
    }
}
