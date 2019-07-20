using System;
using System.IO;
using Juniper.World.GIS;

namespace Juniper.Google.Maps.StreetView
{
    public class MetadataSearch : AbstractStreetViewSearch<Metadata>
    {
        public MetadataSearch(PanoID pano)
            : base("streetview/metadata", "application/json", "json", pano) { }

        public MetadataSearch(PlaceName placeName)
            : base("streetview/metadata", "application/json", "json", placeName) { }

        public MetadataSearch(LatLngPoint location)
            : base("streetview/metadata", "application/json", "json", location) { }

        internal override Func<Stream, Metadata> GetDecoder(AbstractAPI api)
        {
            return api.DecodeObject<Metadata>;
        }
    }
}