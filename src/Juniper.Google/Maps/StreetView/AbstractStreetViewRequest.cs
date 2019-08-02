using Juniper.HTTP.REST;
using Juniper.Serialization;
using Juniper.World.GIS;

namespace Juniper.Google.Maps.StreetView
{
    public abstract class AbstractStreetViewRequest<ResultType> : AbstractMapsRequest<ResultType>
    {
        private PanoID pano;
        private PlaceName placeName;
        private LatLngPoint location;

        protected AbstractStreetViewRequest(AbstractEndpoint api, IDeserializer<ResultType> deserializer, string path)
            : base(api, deserializer, path, true)
        {
        }

        public PanoID Pano
        {
            get { return pano; }
            set { SetLocation(value); }
        }

        public PlaceName Place
        {
            get { return placeName; }
            set { SetLocation(value); }
        }

        public LatLngPoint Location
        {
            get { return location; }
            set { SetLocation(value); }
        }

        public void SetLocation(PanoID location)
        {
            placeName = default;
            this.location = default;
            pano = location;
            cacheLocString = $"pano={location}";
            SetQuery(nameof(pano), (string)location);
        }

        public void SetLocation(PlaceName location)
        {
            placeName = location;
            this.location = default;
            pano = default;
            cacheLocString = $"address={location}";
            SetQuery(nameof(location), (string)location);
        }

        public void SetLocation(LatLngPoint location)
        {
            placeName = default;
            this.location = location;
            pano = default;
            cacheLocString = $"latlng={location}";
            SetQuery(nameof(location), (string)location);
        }
    }
}