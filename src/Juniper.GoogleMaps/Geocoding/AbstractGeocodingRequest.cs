namespace Juniper.World.GIS.Google.Geocoding
{
    public abstract class AbstractGeocodingRequest : AbstractGoogleMapsRequest<MediaType.Application>
    {
        private string language;

        protected AbstractGeocodingRequest(string apiKey)
            : base("geocode/json", Juniper.MediaType.Application.Json, apiKey, null)
        {
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