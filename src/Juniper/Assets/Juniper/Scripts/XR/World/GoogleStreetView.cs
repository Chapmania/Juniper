using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Juniper.Animation;
using Juniper.Google.Maps;
using Juniper.Google.Maps.StreetView;
using Juniper.Imaging.JPEG;
using Juniper.Progress;
using Juniper.Security;
using Juniper.Units;
using Juniper.Unity;
using Juniper.Unity.Coroutines;
using Juniper.World;
using Juniper.World.GIS;

using UnityEngine;
using UnityEngine.Events;
using Yarrow.Client;

namespace Juniper.Imaging
{
    public class GoogleStreetView : SubSceneController, ICredentialReceiver
    {
        private const string LAT_LON = "_MAPPING_LATITUDE_LONGITUDE_LAYOUT";
        private const string SIDES_6 = "_MAPPING_6_FRAMES_LAYOUT";

        public string yarrowServerHost = "http://localhost";
        public string gmapsApiKey;
        public string gmapsSigningKey;

        public TextureFormat textureFormat = TextureFormat.RGB24;
        public Color tint = Color.gray;

        [Range(0, 8)]
        public float exposure = 1;

        [Range(0, 360)]
        public float rotation;

        public bool useMipMap = true;

        private Avatar avatar;

        private YarrowClient<ImageData> yarrow;

        public int searchRadius = 50;

        [ReadOnly]
        public string Location;

        private LatLngPoint LatLngLocation;
        private PanoID curPano;

        [SerializeField]
        [HideInNormalInspector]
        private FadeTransition fader;

        [SerializeField]
        [HideInNormalInspector]
        private GPSLocation gps;

        private bool locked;

        private readonly Dictionary<string, MetadataResponse> metadataCache = new Dictionary<string, MetadataResponse>();
        private readonly Dictionary<PanoID, Transform> panoContainerCache = new Dictionary<PanoID, Transform>();
        private readonly Dictionary<PanoID, Dictionary<int, Transform>> panoDetailContainerCache = new Dictionary<PanoID, Dictionary<int, Transform>>();
        private readonly Dictionary<PanoID, Dictionary<int, Dictionary<int, Transform>>> panoDetailSliceContainerCache = new Dictionary<PanoID, Dictionary<int, Dictionary<int, Transform>>>();
        private readonly Dictionary<PanoID, Dictionary<int, Dictionary<int, Dictionary<int, Transform>>>> panoDetailSliceFrameContainerCache = new Dictionary<PanoID, Dictionary<int, Dictionary<int, Dictionary<int, Transform>>>>();

#if UNITY_EDITOR

        private EditorTextInput locationInput;

        public void OnValidate()
        {
            locationInput = this.Ensure<EditorTextInput>();
            ClearCredentials();
        }

#endif

        public string CredentialFile
        {
            get
            {
                var baseCachePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var keyFile = Path.Combine(baseCachePath, "GoogleMaps", "keys.txt");
                return keyFile;
            }
        }

        public void ReceiveCredentials(string[] args)
        {
            gmapsApiKey = args[0];
            gmapsSigningKey = args[1];
        }

        public void ClearCredentials()
        {
            gmapsApiKey = null;
            gmapsSigningKey = null;
        }

        private void FindComponents()
        {
            fader = ComponentExt.FindAny<FadeTransition>();
            gps = ComponentExt.FindAny<GPSLocation>();
            avatar = ComponentExt.FindAny<Avatar>();
        }

        public override void Awake()
        {
            base.Awake();

            FindComponents();

#if UNITY_EDITOR
            this.ReceiveCredentials();

            locationInput = this.Ensure<EditorTextInput>();
            locationInput.OnSubmit.AddListener(new UnityAction<string>(SetLocation));
            if (!string.IsNullOrEmpty(locationInput.value))
            {
                SetLocation(locationInput.value);
            }

            var baseCachePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
#else
            var baseCachePath = Application.persistentDataPath;
#endif
            var yarrowCacheDirName = Path.Combine(baseCachePath, "Yarrow");
            var yarrowCacheDir = new DirectoryInfo(yarrowCacheDirName);
            var gmapsCacheDirName = Path.Combine(baseCachePath, "GoogleMaps");
            var gmapsCacheDir = new DirectoryInfo(gmapsCacheDirName);
            var uri = new Uri(yarrowServerHost);
            var decoder = new JpegDecoder();
            yarrow = new YarrowClient<ImageData>(uri, decoder, yarrowCacheDir, gmapsApiKey, gmapsSigningKey, gmapsCacheDir);
        }

        public override void Enter(IProgress prog = null)
        {
            base.Enter(prog);
            if (string.IsNullOrEmpty(Location) && gps?.HasCoord == true)
            {
                SetLatLngLocation(gps.Coord);
            }
            GetImages(false, prog);
        }

        private string lastStatus;

        public override void Update()
        {
            base.Update();
            if (yarrow.Status != lastStatus)
            {
                ScreenDebugger.Print(yarrow.Status);
                lastStatus = yarrow.Status;
            }
            if (IsEntered && IsComplete && !locked)
            {
                GetImages(true);
            }
        }

        private Coroutine GetImages(bool fromNavigation, IProgress prog = null)
        {
            locked = true;
            return StartCoroutine(GetImagesCoroutine(fromNavigation, prog));
        }

        private static readonly float[] FOVs =
        {
            90,
            60,
            45,
            30
        };

        private UTMPoint? last;

        private IEnumerator GetImagesCoroutine(bool fromNavigation, IProgress prog)
        {
            if (!string.IsNullOrEmpty(Location))
            {
                var metadataProg = prog.Subdivide(0, 0.1f);
                var subProg = prog.Subdivide(0.1f, 0.9f);

                if (!metadataCache.ContainsKey(Location))
                {
                    Task<MetadataResponse> metadataTask;
                    if (LatLngPoint.TryParseDecimal(Location, out var point))
                    {
                        metadataTask = yarrow.GetMetadata(point, metadataProg);
                    }
                    else
                    {
                        metadataTask = yarrow.GetMetadata((PlaceName)Location, metadataProg);
                    }

                    while (!metadataTask.IsCompleted && !metadataTask.IsCanceled && !metadataTask.IsFaulted)
                    {
                        yield return null;
                    }

                    if (metadataTask.IsCompleted)
                    {
                        var m = metadataTask.Result;
                        metadataCache[Location] = m;

                        if (m.pano_id != curPano)
                        {
                            curPano = m.pano_id;
                            print($"Pano ID = {curPano.ToString()}");
                            if (fromNavigation)
                            {
                                fader.Enter();
                                yield return fader.Waiter;
                            }

                            var cur = m.location.ToUTM();
                            if (last != null)
                            {
                                var delta = 20 * cur.Subtract(last.Value);
                                if (delta.magnitude > 0)
                                {
                                    avatar.transform.position += delta;
                                    transform.position += delta;
                                }
                            }

                            last = cur;
                        }
                    }
                }

                if (metadataCache.ContainsKey(Location))
                {
                    var metadata = metadataCache[Location];

                    if (metadata?.status != HttpStatusCode.OK)
                    {
                        print("no metadata");
                    }
                    else
                    {
                        SetLatLngLocation(metadata.location);

                        var panoid = metadata.pano_id;
                        var euler = avatar.Head.rotation.eulerAngles;
                        var heading = euler.y;
                        var pitch = euler.x;

                        for (var f = 0; f < FOVs.Length; ++f)
                        {
                            var faceProg = subProg.Subdivide(f, FOVs.Length);
                            var subFOV = FOVs[f];
                            var overlap = FOVs.Length - f;
                            var fov = subFOV + 2 * overlap;
                            var radius = 5 * overlap + 2;
                            var scale = 2 * radius * Mathf.Tan(Degrees.Radians(fov / 2));
                            var tileDim = 3;
                            var dTileDim = Mathf.Floor(tileDim / 2.0f);
                            for (var y = 0; y < tileDim; ++y)
                            {
                                var sliceProg = faceProg.Subdivide(y, tileDim);
                                var dy = y - dTileDim;
                                var subPitch = pitch + dy * subFOV;
                                var unityPitch = (int)Mathf.Repeat(subFOV * Mathf.RoundToInt(subPitch / subFOV), 360);
                                var requestPitch = (int)Mathf.Repeat(360 - unityPitch, 360);

                                while (requestPitch > 90)
                                {
                                    requestPitch -= 360;
                                }

                                for (var x = 0; x < tileDim; ++x)
                                {
                                    var patchProg = sliceProg.Subdivide(x, tileDim);
                                    var dx = x - dTileDim;
                                    var subHeading = heading + dx * subFOV;
                                    var requestHeading = (int)Mathf.Repeat(subFOV * Mathf.Round(subHeading / subFOV), 360);

                                    if (requestPitch == 90 || requestPitch == -90)
                                    {
                                        requestHeading = 0;
                                    }

                                    if (0 <= requestHeading && requestHeading < 360
                                        && -90 <= requestPitch && requestPitch <= 90
                                        && FillCaches(panoid, f, radius, requestHeading, requestPitch))
                                    {
                                        var imageTask = yarrow.GetImage(panoid, (int)fov, requestHeading, requestPitch, patchProg.Subdivide(0f, 0.9f));
                                        while (!imageTask.IsCanceled && !imageTask.IsFaulted && !imageTask.IsCompleted)
                                        {
                                            yield return null;
                                        }

                                        if (imageTask.IsCompleted)
                                        {
                                            var image = imageTask.Result;
                                            if (image != null)
                                            {
                                                var textureProg = patchProg.Subdivide(0.9f, 0.1f);
                                                var texture = new Texture2D(image.dimensions.width, image.dimensions.height, TextureFormat.RGB24, false);
                                                if (image.format == ImageFormat.None)
                                                {
                                                    texture.LoadRawTextureData(image.data);
                                                }
                                                else if (image.format != ImageFormat.Unsupported)
                                                {
                                                    texture.LoadImage(image.data);
                                                }
                                                textureProg?.Report(0.3333f);
                                                yield return null;
                                                texture.Compress(true);
                                                textureProg?.Report(0.66667f);
                                                yield return null;
                                                texture.Apply(false, true);
                                                textureProg?.Report(1);

                                                var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
                                                var renderer = frame.GetComponent<MeshRenderer>();
                                                var material = new Material(Shader.Find("Unlit/Texture"));
                                                material.SetTexture("_MainTex", texture);
                                                renderer.SetMaterial(material);

                                                frame.transform.SetParent(panoDetailSliceFrameContainerCache[panoid][f][requestHeading][requestPitch], false);
                                                frame.transform.localScale = scale * Vector3.one;
                                            }
                                        }
                                    }
                                }
                            }

                            if (f == 0)
                            {
                                Complete();

                                if (fromNavigation && fader.IsEntered && fader.IsComplete)
                                {
                                    fader.Exit();
                                }
                            }
                        }
                    }
                }
            }
            locked = false;
        }

        private bool FillCaches(PanoID panoid, int f, float radius, int requestHeading, int requestPitch)
        {
            if (!panoDetailSliceFrameContainerCache.ContainsKey(panoid))
            {
                var pano = new GameObject(panoid.ToString()).transform;
                pano.position = transform.position;
                panoContainerCache[panoid] = pano;
                panoDetailContainerCache[panoid] = new Dictionary<int, Transform>();
                panoDetailSliceContainerCache[panoid] = new Dictionary<int, Dictionary<int, Transform>>();
                panoDetailSliceFrameContainerCache[panoid] = new Dictionary<int, Dictionary<int, Dictionary<int, Transform>>>();
            }

            var panoContainer = panoContainerCache[panoid];
            var detailContainerCache = panoDetailContainerCache[panoid];
            var detailSliceContainerCache = panoDetailSliceContainerCache[panoid];
            var detailSliceFrameContainerCache = panoDetailSliceFrameContainerCache[panoid];

            if (!detailContainerCache.ContainsKey(f))
            {
                var detail = new GameObject(f.ToString()).transform;
                detail.SetParent(panoContainer, false);
                detailContainerCache[f] = detail;
                detailSliceContainerCache[f] = new Dictionary<int, Transform>();
                detailSliceFrameContainerCache[f] = new Dictionary<int, Dictionary<int, Transform>>();
            }

            var detailContainer = detailContainerCache[f];
            var sliceContainerCache = detailSliceContainerCache[f];
            var sliceFrameContainerCache = detailSliceFrameContainerCache[f];

            if (!sliceContainerCache.ContainsKey(requestHeading))
            {
                var slice = new GameObject(requestHeading.ToString()).transform;
                slice.SetParent(detailContainer, false);
                sliceContainerCache[requestHeading] = slice;
                sliceFrameContainerCache[requestHeading] = new Dictionary<int, Transform>();
            }

            var sliceContainer = sliceContainerCache[requestHeading];
            var frameContainerCache = sliceFrameContainerCache[requestHeading];

            if (!frameContainerCache.ContainsKey(requestPitch))
            {
                var frameContainer = new GameObject(requestPitch.ToString()).transform;
                frameContainer.rotation = Quaternion.Euler(-requestPitch, requestHeading, 0);
                frameContainer.position = frameContainer.rotation * (radius * Vector3.forward);
                frameContainer.SetParent(sliceContainer, false);
                frameContainerCache[requestPitch] = frameContainer;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnExiting()
        {
            base.OnExiting();
            Complete();
        }

        public void SetLatLngLocation(LatLngPoint location)
        {
            LatLngLocation = location;
            Location = LatLngLocation.ToString();

            if (gps != null)
            {
                gps.FakeCoord = true;
                gps.Coord = location;
            }
#if UNITY_EDITOR
            if (locationInput != null)
            {
                locationInput.value = Location;
            }
#endif
        }

        public void SetLocation(string location)
        {
            Location = location;
        }

        public void Move(Vector2 deltaMeters)
        {
            if (LatLngLocation != null)
            {
                yarrow.ClearError();
                deltaMeters /= 10f;
                var utm = LatLngLocation.ToUTM();
                utm = new UTMPoint(utm.X + deltaMeters.x, utm.Y + deltaMeters.y, utm.Z, utm.Zone, utm.Hemisphere);
                SetLatLngLocation(utm.ToLatLng());
            }
        }

        public void MoveNorth()
        {
            Move(Vector2.up * 2 * searchRadius);
        }

        public void MoveEast()
        {
            Move(Vector2.right * 2 * searchRadius);
        }

        public void MoveWest()
        {
            Move(Vector2.left * 2 * searchRadius);
        }

        public void MoveSouth()
        {
            Move(Vector2.down * 2 * searchRadius);
        }
    }
}