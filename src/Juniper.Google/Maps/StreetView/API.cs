using System.IO;

using Juniper.Serialization;

namespace Juniper.Google.Maps.StreetView
{
    public class API : AbstractAPI
    {
        public API(IDeserializer deserializer, string apiKey, string signingKey, DirectoryInfo cacheLocation)
            : base(deserializer, apiKey, signingKey, cacheLocation) { }
    }
}