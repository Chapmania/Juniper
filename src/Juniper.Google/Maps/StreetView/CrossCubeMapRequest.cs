using System.IO;
using System.Net;
using System.Threading.Tasks;

using Juniper.HTTP;
using Juniper.Imaging;
using Juniper.Progress;
using Juniper.Serialization;
using Juniper.World.GIS;

namespace Juniper.Google.Maps.StreetView
{
    public class CrossCubeMapRequest<T> : AbstractStreetViewImageRequest<T>
    {
        private readonly GoogleMapsRequestConfiguration gmaps;
        private readonly IImageDecoder<T> decoder;

        public CrossCubeMapRequest(GoogleMapsRequestConfiguration api, IImageDecoder<T> decoder, Size size)
            : base(api, decoder, size)
        {
            gmaps = api;
            this.decoder = decoder;
        }

        public CrossCubeMapRequest(GoogleMapsRequestConfiguration api, IImageDecoder<T> decoder, Size size, PanoID pano)
            : base(api, decoder, size, pano)
        {
            gmaps = api;
            this.decoder = decoder;
        }

        public CrossCubeMapRequest(GoogleMapsRequestConfiguration api, IImageDecoder<T> decoder, Size size, PlaceName placeName)
            : base(api, decoder, size, placeName)
        {
            gmaps = api;
            this.decoder = decoder;
        }

        public CrossCubeMapRequest(GoogleMapsRequestConfiguration api, IImageDecoder<T> decoder, Size size, LatLngPoint location)
            : base(api, decoder, size, location)
        {
            gmaps = api;
            this.decoder = decoder;
        }

        protected override string CacheFileName
        {
            get
            {
                var fileName = base.CacheFileName;
                var extension = Path.GetExtension(fileName);
                return Path.ChangeExtension(fileName, "cubemap" + extension);
            }
        }

        public async Task<T> GetJPEG(IProgress prog = null)
        {
            if (!IsCached)
            {
                await Get(prog);
            }

            return decoder.Read(CacheFile.FullName);
        }

        public async Task ProxyJPEG(HttpListenerResponse response, IProgress prog = null)
        {
            if (!IsCached)
            {
                await Get(prog);
            }

            response.SendFile(CacheFile);
        }

        public override async Task<T> Get(IProgress prog = null)
        {
            var cacheFile = CacheFile;
            if (IsCached)
            {
                return deserializer.Load(cacheFile, prog);
            }
            else
            {
                var progs = prog.Split(3);
                var subRequest = new CubeMapRequest<T>(gmaps, decoder, Size);

                if (Pano != default)
                {
                    subRequest.Pano = Pano;
                }
                else if (Place != default)
                {
                    subRequest.Place = Place;
                }
                else if (Location != default)
                {
                    subRequest.Location = Location;
                }

                if (Radius != default)
                {
                    subRequest.Radius = Radius;
                }

                subRequest.OutdoorOnly = OutdoorOnly;
                var images = await subRequest.Get(progs[0]);
                var combined = await decoder.CombineCross(images[0], images[1], images[2], images[3], images[4], images[5], progs[1]);
                if (cacheFile != null)
                {
                    decoder.Save(cacheFile, combined);
                }
                progs[2]?.Report(1);
                return combined;
            }
        }
    }
}