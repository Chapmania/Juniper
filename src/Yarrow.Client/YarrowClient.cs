using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

using Juniper.Google.Maps;
using Juniper.Google.Maps.Geocoding;
using Juniper.Google.Maps.StreetView;
using Juniper.HTTP.REST;
using Juniper.Imaging;
using Juniper.Progress;
using Juniper.World.GIS;

namespace Yarrow.Client
{
    public class YarrowClient<T>
    {
        private readonly YarrowRequestConfiguration yarrow;
        private readonly YarrowMetadataRequest yarrowMetadataRequest;
        private readonly YarrowImageRequest<T> yarrowImageRequest;
        private readonly YarrowGeocodingRequest yarrowReverseGeocodeRequest;

        private readonly GoogleMapsRequestConfiguration gmaps;
        private readonly MetadataRequest gmapsMetadataRequest;
        private readonly CrossCubeMapRequest<T> gmapsImageRequest;
        private readonly ReverseGeocodingRequest gmapsReverseGeocodeRequest;

        private bool useGoogleMaps = false;

        public YarrowClient(Uri yarrowServerUri, IImageDecoder<T> decoder, DirectoryInfo yarrowCacheDir)
        {
            yarrow = new YarrowRequestConfiguration(yarrowServerUri, yarrowCacheDir);
            yarrowMetadataRequest = new YarrowMetadataRequest(yarrow);
            yarrowImageRequest = new YarrowImageRequest<T>(yarrow, decoder);
            yarrowReverseGeocodeRequest = new YarrowGeocodingRequest(yarrow);
        }

        public YarrowClient(Uri yarrowServerUri, IImageDecoder<T> decoder, DirectoryInfo yarrowCacheDir, string gmapsApiKey, string gmapsSigningKey, DirectoryInfo gmapsCacheDir)
            : this(yarrowServerUri, decoder, yarrowCacheDir)
        {
            gmaps = new GoogleMapsRequestConfiguration(gmapsApiKey, gmapsSigningKey, gmapsCacheDir);
            gmapsMetadataRequest = new MetadataRequest(gmaps);
            gmapsImageRequest = new CrossCubeMapRequest<T>(gmaps, decoder, new Size(640, 640));
            gmapsReverseGeocodeRequest = new ReverseGeocodingRequest(gmaps);
        }

        private Task<ResultT> Cascade<YarrowRequestT, GmapsRequestT, ResultT>(
            YarrowRequestT yarrowRequest,
            GmapsRequestT gmapsRequest,
            Func<AbstractRequest<ResultT>, IProgress, Task<ResultT>> getter,
            IProgress prog)
            where YarrowRequestT : AbstractRequest<ResultT>
            where GmapsRequestT : AbstractRequest<ResultT>
        {
            return Task.Run(async () =>
            {
                if (!useGoogleMaps)
                {
                    try
                    {
                        return await getter(yarrowRequest, prog);
                    }
                    catch (WebException)
                    {
                        useGoogleMaps = true;
                    }
                }

                if (useGoogleMaps)
                {
                    return await getter(gmapsRequest, prog);
                }
                else
                {
                    return default;
                }
            });
        }

        public Task<MetadataResponse> GetMetadata(PanoID pano, IProgress prog = null)
        {
            yarrowMetadataRequest.Pano = gmapsMetadataRequest.Pano = pano;
            return Cascade<YarrowMetadataRequest, MetadataRequest, MetadataResponse>
                (yarrowMetadataRequest, gmapsMetadataRequest, (req, p) => req.Get(p), prog);
        }

        public Task<MetadataResponse> GetMetadata(PlaceName placeName, IProgress prog = null)
        {
            yarrowMetadataRequest.Place = gmapsMetadataRequest.Place = placeName;
            return Cascade<YarrowMetadataRequest, MetadataRequest, MetadataResponse>
                (yarrowMetadataRequest, gmapsMetadataRequest, (req, p) => req.Get(p), prog);
        }

        public Task<MetadataResponse> GetMetadata(LatLngPoint latLng, IProgress prog = null)
        {
            yarrowMetadataRequest.Location = gmapsMetadataRequest.Location = latLng;
            return Cascade<YarrowMetadataRequest, MetadataRequest, MetadataResponse>
                (yarrowMetadataRequest, gmapsMetadataRequest, (req, p) => req.Get(p), prog);
        }

        public Task<T> GetImage(PanoID pano, IProgress prog = null)
        {
            yarrowImageRequest.Pano = gmapsImageRequest.Pano = pano;
            return Cascade<YarrowImageRequest<T>, CrossCubeMapRequest<T>, T>
                (yarrowImageRequest, gmapsImageRequest, (req, p) => req.GetJPEG(p), prog);
        }

        public Task<GeocodingResponse> ReverseGeocode(LatLngPoint latLng, IProgress prog = null)
        {
            yarrowReverseGeocodeRequest.Location = gmapsReverseGeocodeRequest.Location = latLng;
            return Cascade<YarrowGeocodingRequest, ReverseGeocodingRequest, GeocodingResponse>
                (yarrowReverseGeocodeRequest, gmapsReverseGeocodeRequest, (req, p) => req.Get(p), prog);
        }
    }
}