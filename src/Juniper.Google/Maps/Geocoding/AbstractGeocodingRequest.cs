using System.IO;
using Juniper.HTTP;

namespace Juniper.Google.Maps.Geocoding
{
    public abstract class AbstractGeocodingRequest : AbstractGoogleMapsRequest
    {
        private string language;

        protected AbstractGeocodingRequest(string apiKey)
            : base("geocode/json", apiKey, MediaType.Application.Json)
        {
        }

        public override string CacheID
        {
            get
            {
                return Path.Combine("geocoding", base.CacheID);
            }
        }

        public string Language
        {
            get { return language; }
            set
            {
                language = value;
                SetQuery(nameof(language), language);
            }
        }
    }
}