using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Juniper.Animation;
using Juniper.Display;
using Juniper.GIS.Google.Geocoding;
using Juniper.GIS.Google.StreetView;
using Juniper.Imaging;
using Juniper.Imaging.Unity;
using Juniper.Input;
using Juniper.IO;
using Juniper.Progress;
using Juniper.Security;
using Juniper.Units;
using Juniper.Unity;
using Juniper.Widgets;

using UnityEngine;
using UnityEngine.Events;

namespace Juniper.World.GIS.Google
{
    public class GoogleStreetView : SubSceneController
#if UNITY_EDITOR
        , ICredentialReceiver
#endif
    {
        private static readonly Regex GMAPS_URL_PANO_PATTERN =
            new Regex("https?://www\\.google\\.com/maps/@-?\\d+\\.\\d+,-?\\d+\\.\\d+(?:,[a-zA-Z0-9.]+)*/data=(?:![a-z0-9]+)*!1s([a-zA-Z0-9_\\-]+)(?:![a-z0-9]+)*", RegexOptions.Compiled);

        private static readonly Regex GMAPS_URL_LATLNG_PATTERN =
            new Regex("https?://www\\.google\\.com/maps/@(-?\\d+\\.\\d+,-?\\d+\\.\\d+)", RegexOptions.Compiled);

        private readonly Dictionary<string, MetadataResponse> metadataCache = new Dictionary<string, MetadataResponse>();

        [SerializeField]
        [HideInInspector]
        private string gmapsApiKey;

        [SerializeField]
        [HideInInspector]
        private string gmapsSigningKey;

        public int searchRadius = 50;

        public float[] searchFOVs =
        {
            90,
            60,
            30
        };

        [ReadOnly]
        public string searchLocation;
        private string lastSearchLocation = string.Empty;

        private MetadataResponse metadata;
        private bool locked;
        private bool cubemapLock;


        private Vector3 origin;
        private Vector3 navPointerPosition;

        private GoogleMapsClient<Texture2D> gmaps;
        private FadeTransition fader;
        private GPSLocation gps;
        private PhotosphereManager photospheres;
        private Clickable navPlane;
        private Avatar avatar;
        private Transform navPointer;
        private UnifiedInputModule input;
        private CachingStrategy cache;
        private IImageCodec<Texture2D> codec;

#if UNITY_EDITOR
        private EditorTextInput locationInput;

        public void OnValidate()
        {
            locationInput = this.Ensure<EditorTextInput>();
        }

        public string CredentialFile
        {
            get
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var keyFile = Path.Combine(userProfile, "Projects", "DevKeys", "google-streetview.txt");
                return keyFile;
            }
        }

        public void ReceiveCredentials(string[] args)
        {
            if (args == null)
            {
                gmapsApiKey = null;
                gmapsSigningKey = null;
            }
            else
            {
                gmapsApiKey = args[0];
                gmapsSigningKey = args[1];
            }
        }
#endif

        public override void Awake()
        {
            base.Awake();

            Find.Any(out fader);
            Find.Any(out gps);
            if (!this.FindClosest(out photospheres))
            {
                photospheres = this.Ensure<PhotosphereManager>();
            }

            navPlane = transform.Find("NavPlane").Ensure<Clickable>();
            Find.Any(out avatar);
            navPointer = transform.Find("NavPointer");
            Find.Any(out input);

#if UNITY_EDITOR
            this.ReceiveCredentials();

            locationInput = this.Ensure<EditorTextInput>();
            locationInput.OnSubmit.AddListener(new UnityAction<string>(SetLocation));
            if (!string.IsNullOrEmpty(locationInput.value))
            {
                SetLocation(locationInput.value);
            }
            else if (gps != null && gps.HasCoord)
            {
                SetLocation(gps.Coord.ToString());
            }

            var baseCachePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
#else
            if(gps != null && gps.HasCoord)
            {
                SetLocation(gps.Coord.ToString());
            }
            var baseCachePath = Application.persistentDataPath;
#endif
            cache = new GoogleMapsCachingStrategy(baseCachePath);
            codec = new UnityTextureCodec();
            var metadataDecoder = new JsonFactory<MetadataResponse>();
            var geocodingDecoder = new JsonFactory<GeocodingResponse>();

            gmaps = new GoogleMapsClient<Texture2D>(gmapsApiKey, gmapsSigningKey, codec, metadataDecoder, geocodingDecoder, cache);

            photospheres.CubemapNeeded += Photospheres_CubemapNeeded;
            photospheres.ImageNeeded += Photospheres_ImageNeeded;
            photospheres.PhotosphereReady += Photospheres_PhotosphereReady;
#if UNITY_EDITOR
            photospheres.PhotosphereComplete += Photospheres_PhotosphereComplete;
#endif
            photospheres.SetIO(
                cache,
                new UnityTextureCodec(80));

            photospheres.SetDetailLevels(searchFOVs);

            navPlane.Click += NavPlane_Click;
            var renderer = navPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

#if UNITY_EDITOR

        private static readonly string[] CAPTURE_CUBEMAP_FIELDS = {
            "Rendering cubemap",
            "Copying cubemap faces",
            "Concatenating faces",
            "Saving image"
        };

        private static readonly CubemapFace[] CAPTURE_CUBEMAP_FACES = new[] {
            CubemapFace.NegativeY,
            CubemapFace.NegativeX,
            CubemapFace.PositiveZ,
            CubemapFace.PositiveX,
            CubemapFace.NegativeZ,
            CubemapFace.PositiveY
        };

        private static readonly Texture2D[] CAPTURE_CUBEMAP_SUB_IMAGES = new Texture2D[CAPTURE_CUBEMAP_FACES.Length];

        private readonly Queue<Photosphere> captureQ = new Queue<Photosphere>();

        private void Photospheres_PhotosphereComplete(Photosphere obj)
        {
            captureQ.Enqueue(obj);
            if (!cubemapLock)
            {
                cubemapLock = true;
                this.Run(CaptureCubemapCoroutine());
            }
        }

        private IEnumerator CaptureCubemapCoroutine()
        {
            if (captureQ.Count == 0)
            {
                cubemapLock = false;
            }
            else
            {
                Debug.Log("Capturing Cubemap");
                var photosphere = captureQ.Dequeue();
                var cubemapName = photosphere.name;
                using (var prog = new UnityEditorProgressDialog("Saving cubemap " + cubemapName))
                {
                    var subProgs = prog.Split(CAPTURE_CUBEMAP_FIELDS);

                    subProgs[0].Report(0);
                    const int dim = 2048;
                    var cubemap = new Cubemap(dim, TextureFormat.RGB24, false);
                    cubemap.Apply();

                    var curMask = DisplayManager.MainCamera.cullingMask;
                    DisplayManager.MainCamera.cullingMask = LayerMask.GetMask(Photosphere.PHOTOSPHERE_LAYER_ARR);

                    var curRotation = DisplayManager.MainCamera.transform.rotation;
                    DisplayManager.MainCamera.transform.rotation = Quaternion.identity;

                    DisplayManager.MainCamera.RenderToCubemap(cubemap, 63);

                    DisplayManager.MainCamera.cullingMask = curMask;
                    DisplayManager.MainCamera.transform.rotation = curRotation;
                    subProgs[0].Report(1);

                    var anyDestroyed = false;
                    foreach (var texture in CAPTURE_CUBEMAP_SUB_IMAGES)
                    {
                        if (texture != null)
                        {
                            anyDestroyed = true;
                            Destroy(texture);
                        }
                    }

                    if (anyDestroyed)
                    {
                        yield return JuniperSystem.Cleanup();
                    }

                    for (var f = 0; f < CAPTURE_CUBEMAP_FACES.Length; ++f)
                    {
                        subProgs[1].Report(f, CAPTURE_CUBEMAP_FACES.Length, CAPTURE_CUBEMAP_FACES[f].ToString());
                        try
                        {
                            var pixels = cubemap.GetPixels(CAPTURE_CUBEMAP_FACES[f]);
                            var texture = new Texture2D(cubemap.width, cubemap.height);
                            texture.SetPixels(pixels);
                            texture.Apply();
                            CAPTURE_CUBEMAP_SUB_IMAGES[f] = texture;
                        }
                        catch (Exception exp)
                        {
                            Debug.LogException(exp);
                            cubemapLock = false;
                            throw;
                        }
                        subProgs[1].Report(f + 1, CAPTURE_CUBEMAP_FACES.Length);
                        yield return null;
                    }

                    try
                    {
                        var img = codec.Concatenate(ImageData.CubeCross(CAPTURE_CUBEMAP_SUB_IMAGES), subProgs[2]);
                        var cubemapRef = new ContentReference<MediaType.Image>(cubemapName, codec.ContentType);
                        cache.Save(cubemapRef, img, codec, subProgs[3]);
                        Debug.Log("Cubemap saved " + cubemapName);
                    }
                    catch (Exception exp)
                    {
                        Debug.LogException(exp);
                        cubemapLock = false;
                        throw;
                    }
                }

                this.Run(CaptureCubemapCoroutine());
            }
        }
#endif

        private string Photospheres_CubemapNeeded(Photosphere source)
        {
            return $"{source.name}.jpeg";
        }

        private Task<Texture2D> Photospheres_ImageNeeded(Photosphere source, int fov, int heading, int pitch)
        {
            return gmaps.GetImage(source.name, fov, heading, pitch);
        }

        public override void Enter(IProgress prog)
        {
            base.Enter(prog);
            SynchronizeData(Vector3.zero, prog);
        }

        public void Update()
        {
            if (IsEntered && IsComplete && !locked)
            {
                var point = input.MouseEnabled
                    ? input.Mouse.CursorPosition
                    : input.ControllersEnabled
                        ? input.Controllers[0].IsConnected
                            ? input.Controllers[0].CursorPosition
                            : input.Controllers[1].CursorPosition
                        : input.GazeEnabled
                            ? input.Gaze.CursorPosition
                            : Vector3.zero;

                SynchronizeData(point);
            }

            navPointer.position = navPointerPosition;
        }

        private void SynchronizeData(Vector3 cursorPosition)
        {
            SynchronizeData(cursorPosition, null);
        }

        private void SynchronizeData(Vector3 cursorPosition, IProgress prog)
        {
            locked = true;
            this.Run(SynchronizeDataCoroutine(cursorPosition, prog));
        }

        private IEnumerator SynchronizeDataCoroutine(Vector3 cursorPosition, IProgress prog)
        {
            string searchPano = null;
            LatLngPoint searchPoint = null;
            var panoMatch = GMAPS_URL_PANO_PATTERN.Match(searchLocation);
            var latLngMatch = GMAPS_URL_LATLNG_PATTERN.Match(searchLocation);

            if (panoMatch.Success && MetadataResponse.IsPano(panoMatch.Groups[1].Value))
            {
                searchPano = panoMatch.Groups[1].Value;
            }

            if (latLngMatch.Success && LatLngPoint.TryParseDecimal(latLngMatch.Groups[1].Value, out var point))
            {
                searchPoint = point;
            }

            var loadImmediately = true;
            if (lastSphere != null && searchLocation == lastSearchLocation && metadata != null)
            {
                var nextVec = cursorPosition + origin;
                nextVec.y = 0;
                var curUTM = LatLng.ToUTM(metadata.location);
                var nextPoint = nextVec.ToUTM(curUTM.Zone, curUTM.Hemisphere).ToLatLng();
                searchLocation = nextPoint.ToString();
                loadImmediately = false;
            }

            if (!string.IsNullOrEmpty(searchLocation) && searchLocation != lastSearchLocation)
            {
                var metadataProg = prog.Subdivide(0, 0.1f);
                var subProg = prog.Subdivide(0.1f, 0.9f);
                if (metadataCache.ContainsKey(searchLocation))
                {
                    yield return CacheMetadata(loadImmediately, subProg);
                }
                else
                {
                    var metaSubProgs = metadataProg.Split("Searching by PanoID", "Searching by Lat/Lng", "Searching by Location Name");
                    metadata = null;
                    yield return RequestMetadata(searchPano, metaSubProgs[0]);
                    if (metadata == null)
                    {
                        yield return RequestMetadata(searchPoint, metaSubProgs[1]);
                    }

                    if (metadata == null)
                    {
                        yield return SearchMetadata(searchLocation, metaSubProgs[2]);
                    }

                    if (metadata != null)
                    {
                        yield return CacheMetadata(loadImmediately, subProg);
                    }
                }
            }

            locked = false;
        }

        private IEnumerator RequestMetadata(string searchPano, IProgress metadataProg)
        {
            if (searchPano != null)
            {
                yield return ValidateMetadata(gmaps.GetMetadata(searchPano, searchRadius, metadataProg));
            }
        }

        private IEnumerator RequestMetadata(LatLngPoint searchPoint, IProgress metadataProg)
        {
            if (searchPoint != null)
            {
                yield return ValidateMetadata(gmaps.GetMetadata(searchPoint, searchRadius, metadataProg));
            }
        }

        private IEnumerator SearchMetadata(string searchLocation, IProgress metadataProg)
        {
            if (!string.IsNullOrEmpty(searchLocation))
            {
                yield return ValidateMetadata(gmaps.SearchMetadata(searchLocation, searchRadius, metadataProg));
            }
        }

        private IEnumerator ValidateMetadata(Task<MetadataResponse> metadataTask)
        {
            yield return metadataTask.AsCoroutine();

            metadata = metadataTask.Result;
            if (metadata?.status != HttpStatusCode.OK)
            {
                metadata = null;
            }
        }

        private IEnumerator CacheMetadata(bool loadImmediately, IProgress subProg)
        {
            metadataCache[metadata.pano_id] = metadata;
            metadataCache[metadata.location.ToString()] = metadata;
            metadataCache[searchLocation] = metadata;
            lastSearchLocation = searchLocation;
            if (gps != null)
            {
                gps.FakeCoord = true;
                gps.Coord = metadata.location;
            }
#if UNITY_EDITOR
            if (locationInput != null)
            {
                locationInput.value = searchLocation;
            }
#endif

            var nextVec = metadata.location.ToVector3();
            if (lastSphere == null)
            {
                origin = nextVec;
            }
            else if (!loadImmediately)
            {
                navPointerPosition = nextVec - origin;
            }

            if (loadImmediately)
            {
                yield return LoadPhotosphereCoroutine(subProg);
            }
        }

        private void NavPlane_Click(object sender, EventArgs e)
        {
            if (!locked)
            {
                locked = true;
                this.Run(LoadPhotosphereCoroutine());
            }
        }

        private Photosphere lastSphere;
        private IEnumerator LoadPhotosphereCoroutine()
        {
            return LoadPhotosphereCoroutine(null);
        }

        private IEnumerator LoadPhotosphereCoroutine(IProgress prog)
        {
            var curSphere = photospheres.GetPhotosphere<Photosphere>(metadata.pano_id);

            if (lastSphere != null)
            {
                yield return fader.EnterCoroutine();
                lastSphere.Deactivate();
            }

            curSphere.Activate();

            while (!curSphere.IsReady)
            {
                prog?.Report(curSphere.ProgressToReady, "Loading photosphere");
                yield return null;
            }

            if (lastSphere != null)
            {
                yield return fader.ExitCoroutine();
            }
            else
            {
                Complete();
            }

            locked = false;
            lastSphere = curSphere;
        }

        private void Photospheres_PhotosphereReady(Photosphere obj)
        {
            var nextVec = metadata.location.ToVector3();
            var delta = nextVec - origin;
            navPlane.transform.position = avatar.transform.position = delta;
            obj.transform.position = avatar.Head.position;
        }

        protected override void OnExiting()
        {
            base.OnExiting();
            Complete();
        }

        public void SetLocation(string location)
        {
            searchLocation = location;
        }
    }
}