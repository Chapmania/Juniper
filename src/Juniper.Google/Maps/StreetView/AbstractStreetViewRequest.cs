using System.IO;
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

        private AbstractStreetViewRequest(AbstractEndpoint api, IDeserializer<ResultType> deserializer, string path, string key)
            : base(api, deserializer, path, Path.Combine("streetview", key), true)
        {
        }

        protected AbstractStreetViewRequest(AbstractEndpoint api, IDeserializer<ResultType> deserializer, string path, PanoID location)
            : this(api, deserializer, path, $"pano={location}")
        {
            SetLocation(location);
        }

        protected AbstractStreetViewRequest(AbstractEndpoint api, IDeserializer<ResultType> deserializer, string path, PlaceName location)
            : this(api, deserializer, path, $"address={location}")
        {
            SetLocation(location);
        }

        protected AbstractStreetViewRequest(AbstractEndpoint api, IDeserializer<ResultType> deserializer, string path, LatLngPoint location)
            : this(api, deserializer, path, $"latlng={location}")
        {
            SetLocation(location);
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
            SetQuery(nameof(pano), (string)location);
        }

        public void SetLocation(PlaceName location)
        {
            placeName = location;
            this.location = default;
            pano = default;
            SetQuery(nameof(location), (string)location);
        }

        public void SetLocation(LatLngPoint location)
        {
            placeName = default;
            this.location = location;
            pano = default;
            SetQuery(nameof(location), (string)location);
        }
    }
}