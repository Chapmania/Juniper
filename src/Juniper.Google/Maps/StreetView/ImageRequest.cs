using System;
using System.IO;
using Juniper.HTTP.REST;
using Juniper.Image;
using Juniper.World;
using Juniper.World.GIS;

namespace Juniper.Google.Maps.StreetView
{
    public class ImageRequest : AbstractImageRequest
    {
        public static ImageRequest Create(LocationTypes locationType, object value, Size size)
        {
            switch (locationType)
            {
                case LocationTypes.PanoID: return new ImageRequest((PanoID)value, size);
                case LocationTypes.PlaceName: return new ImageRequest((PlaceName)value, size);
                case LocationTypes.LatLngPoint: return new ImageRequest((LatLngPoint)value, size);
                default: return default;
            }
        }

        public static ImageRequest Create(LocationTypes locationType, object value, int width, int height)
        {
            return Create(locationType, value, new Size(width, height));
        }

        private Size size;
        private Heading heading;
        private Pitch pitch;

        public ImageRequest(PanoID pano, Size size)
            : base(pano, size) { }

        public ImageRequest(PanoID pano, int width, int height)
            : base(pano, width, height) { }

        public ImageRequest(PlaceName placeName, Size size)
            : base(placeName, size) { }

        public ImageRequest(PlaceName placeName, int width, int height)
            : base(placeName, width, height) { }

        public ImageRequest(LatLngPoint location, Size size)
            : base(location, size) { }

        public ImageRequest(LatLngPoint location, int width, int height)
            : base(location, width, height) { }

        
        public Heading Heading
        {
            get { return heading; }
            set
            {
                heading = value;
                SetQuery(nameof(heading), (int)value);
            }
        }

        public Pitch Pitch
        {
            get { return pitch; }
            set {
                pitch = value;
                SetQuery(nameof(pitch), (int)value);
            }
        }
    }
}