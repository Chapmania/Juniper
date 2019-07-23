using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Juniper.HTTP.REST;
using Juniper.Image;
using Juniper.World.GIS;

namespace Juniper.Google.Maps.MapTiles
{
    public partial class TileSearch : AbstractMapsSearch<RawImage>
    {
        public static TileSearch Create(LocationTypes locationType, object value, int zoom, Size size, TileImageFormat format = TileImageFormat.PNG8)
        {
            switch (locationType)
            {
                case LocationTypes.LatLngPoint: return new TileSearch((LatLngPoint)value, zoom, size, format);
                case LocationTypes.PlaceName: return new TileSearch((PlaceName)value, zoom, size, format);
                default: return default;
            }
        }

        public static TileSearch Create(LocationTypes locationType, object value, int zoom, int width, int height, TileImageFormat format = TileImageFormat.PNG8)
        {
            return Create(locationType, value, zoom, new Size(width, height), format);
        }

        private readonly List<Marker> markers = new List<Marker>();
        private LinePath path;

        private TileSearch(string center, int zoom, Size size, ImageFormat format)
            : base("staticmap", "tiles", format.contentType, format.fileExtension, true)
        {
            SetQuery(nameof(center), center);
            SetQuery(nameof(zoom), zoom);
            SetQuery(nameof(size), size);
            if (format != FORMAT_DESCRIPTIONS[TileImageFormat.PNG8])
            {
                SetQuery(nameof(format), format.gmapsFieldValue);
            }
        }

        private TileSearch(string center, int zoom, Size size, TileImageFormat format = TileImageFormat.PNG8)
            : this(center, zoom, size, FORMAT_DESCRIPTIONS.Get(format, FORMAT_DESCRIPTIONS[TileImageFormat.PNG8])) { }

        public TileSearch(PlaceName address, int zoom, Size size, TileImageFormat format = TileImageFormat.PNG8)
            : this((string)address, zoom, size, FORMAT_DESCRIPTIONS[format]) { }

        public TileSearch(PlaceName address, int zoom, int width, int height, TileImageFormat format = TileImageFormat.PNG8)
            : this((string)address, zoom, new Size(width, height), format) { }

        public TileSearch(LatLngPoint center, int zoom, Size size, TileImageFormat format = TileImageFormat.PNG8)
            : this(center.ToCSV(), zoom, size, format) { }

        public TileSearch(LatLngPoint center, int zoom, int width, int height, TileImageFormat format = TileImageFormat.PNG8)
            : this(center.ToCSV(), zoom, new Size(width, height), format) { }

        public bool FlipImage { get; set; }

        public override Func<Stream, RawImage> GetDecoder(AbstractEndpoint _)
        {
            return stream => Image.Decoder.DecodePNG(FlipImage, stream);
        }

        public void SetScale(int scale)
        {
            SetQuery(nameof(scale), scale);
        }

        public void SetLanguage(string language)
        {
            SetQuery(nameof(language), language);
        }

        public void SetRegion(string region)
        {
            SetQuery(nameof(region), region);
        }

        public void SetMapType(MapImageType maptype)
        {
            SetQuery(nameof(maptype), maptype);
        }

        public void AddMarker(Marker marker)
        {
            if (marker != default)
            {
                markers.Add(marker);
            }
        }

        public void SetPath(LinePath path)
        {
            this.path = path;
        }

        public override Uri BaseURI
        {
            get
            {
                if (markers.Count > 0)
                {
                    RemoveQuery(nameof(markers));
                    var groupedByStyle = markers.GroupBy(m => m.style);
                    foreach (var group in groupedByStyle)
                    {
                        AddMarkerGroup(group.Key, group);
                    }
                }

                if (path != null)
                {
                    SetQuery(nameof(path), path);
                }

                return base.BaseURI;
            }
        }

        private void AddMarkerGroup(MarkerStyle style, IEnumerable<Marker> group)
        {
            var sb = new StringBuilder();
            var delim = "";
            if (style != default)
            {
                sb.Append(style);
                delim = "|";
            }

            foreach (var point in group)
            {
                sb.Append($"{delim}{point.center}");
                delim = "|";
            }

            AddQuery(nameof(markers), sb);
        }
    }
}