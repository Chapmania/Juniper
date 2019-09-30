using System.IO;

using Juniper.HTTP;
using Juniper.World.GIS;

namespace Juniper.Google.Maps.StreetView
{
    public abstract class AbstractStreetViewRequest : AbstractGoogleMapsRequest
    {
        private string pano;
        private string placeName;
        private LatLngPoint location;
        private int radius;

        protected AbstractStreetViewRequest(string path, string apiKey, string signingKey, MediaType contentType)
            : base(path, apiKey, signingKey, contentType)
        { }

        public override string CacheID
        {
            get
            {
                return Path.Combine("streetview", base.CacheID);
            }
        }

        public string Pano
        {
            get { return pano; }
            set
            {
                placeName = default;
                location = default;
                pano = value;
                RemoveQuery(nameof(location));
                SetQuery(nameof(pano), value);
            }
        }

        public string Place
        {
            get { return placeName; }
            set
            {
                placeName = value;
                location = default;
                pano = default;
                RemoveQuery(nameof(pano));
                SetQuery(nameof(location), value);
            }
        }

        public LatLngPoint Location
        {
            get { return location; }
            set
            {
                placeName = default;
                location = value;
                pano = default;
                RemoveQuery(nameof(pano));
                SetQuery(nameof(location), value.ToString());
            }
        }

        public int Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                SetQuery(nameof(radius), radius);
            }
        }
    }
}