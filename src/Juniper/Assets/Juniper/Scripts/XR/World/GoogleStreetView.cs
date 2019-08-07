using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Juniper.Animation;
using Juniper.Google.Maps;
using Juniper.Google.Maps.StreetView;
using Juniper.Progress;
using Juniper.Units;
using Juniper.Unity;
using Juniper.Unity.Coroutines;
using Juniper.World;
using Juniper.World.GIS;

using UnityEngine;
using Yarrow.Client;

namespace Juniper.Images
{
    public class GoogleStreetView : SubSceneController
    {
        private const string LAT_LON = "_MAPPING_LATITUDE_LONGITUDE_LAYOUT";
        private const string SIDES_6 = "_MAPPING_6_FRAMES_LAYOUT";

        public TextureFormat textureFormat = TextureFormat.RGB24;
        public Color tint = Color.gray;

        [Range(0, 8)]
        public float exposure = 1;

        [Range(0, 360)]
        public float rotation;

        public bool useMipMap = true;

        private Material skyboxMaterial;

        private YarrowClient yarrow;

        public int searchRadius = 50;

        private string lastLocation;

        [ReadOnly]
        public string Location;

        private LatLngPoint LatLngLocation;
        private PanoID pano;

        [SerializeField]
        [HideInNormalInspector]
        private FadeTransition fader;

        [SerializeField]
        [HideInNormalInspector]
        private GPSLocation gps;

        private bool locked;

        private readonly Dictionary<PanoID, Texture> textureCache = new Dictionary<PanoID, Texture>();

#if UNITY_EDITOR

        private EditorTextInput locationInput;

        public void OnValidate()
        {
            locationInput = this.Ensure<EditorTextInput>();
            FindComponents();
        }

#endif

        private void FindComponents()
        {
            fader = ComponentExt.FindAny<FadeTransition>();
            gps = ComponentExt.FindAny<GPSLocation>();
        }

        public override void Awake()
        {
            base.Awake();

            FindComponents();

#if UNITY_EDITOR
            locationInput = this.Ensure<EditorTextInput>();
            locationInput.OnSubmit.AddListener(SetLocation);
            if (!string.IsNullOrEmpty(locationInput.value))
            {
                SetLocation(locationInput.value);
            }
#endif

            yarrow = new YarrowClient();
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

        public override void Update()
        {
            base.Update();

            if (IsEntered && !locked)
            {
                if (Location != lastLocation)
                {
                    GetImages(true);
                }
                else if (Location == lastLocation
                    && skyboxMaterial != null)
                {
                    UpdateSkyBox();
                }
            }
        }

        private Coroutine GetImages(bool fromNavigation, IProgress prog = null)
        {
            locked = true;
            lastLocation = Location;
            if (fromNavigation)
            {
                fader.Enter();
            }
            return StartCoroutine(GetImagesCoroutine(fromNavigation, prog));
        }

        private IEnumerator GetImagesCoroutine(bool fromNavigation, IProgress prog)
        {
            if (!string.IsNullOrEmpty(Location))
            {
                if (fader.IsRunning)
                {
                    yield return fader.Waiter;
                }

                var metadataProg = prog.Subdivide(0, 0.1f);
                var imageProg = prog.Subdivide(0.1f, 0.8f);
                var textureProg = prog.Subdivide(0.9f, 0.1f);
                Task<MetadataResponse> metadataTask;
                if (LatLngPoint.TryParseDecimal(Location, out var point))
                {
                    metadataTask = yarrow.GetMetadata(point, metadataProg);
                }
                else
                {
                    metadataTask = yarrow.GetMetadata((PlaceName)Location, metadataProg);
                }

                yield return new WaitForTask(metadataTask);
                var metadata = metadataTask.Result;

                if (metadata?.status != HttpStatusCode.OK)
                {
                    print("no metadata");
                }
                else
                {
                    print($"Pano ID = {metadata.pano_id}");
                    SetLatLngLocation(metadata.location);
                    pano = metadata.pano_id;
                    lastLocation = Location;

                    if (!textureCache.ContainsKey(metadata.pano_id))
                    {
                        var imageTask = yarrow.GetImage(metadata.pano_id, imageProg);
                        yield return new WaitForTask(imageTask);
                        var image = imageTask.Result;

                        textureProg?.Report(0f);
                        var texture = new Texture2D(image.dimensions.width, image.dimensions.height, TextureFormat.RGB24, false);
                        if (image.format == Image.ImageFormat.None)
                        {
                            texture.LoadRawTextureData(image.data);
                        }
                        else if (image.format != Image.ImageFormat.Unsupported)
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

                        textureCache[metadata.pano_id] = texture;
                    }

                    SetSkyboxTexture(textureCache[metadata.pano_id]);
                }

                locked = false;
                Complete();
                if (fromNavigation)
                {
                    fader.Exit();
                }
            }
        }

        protected override void OnExiting()
        {
            base.OnExiting();
            Complete();
        }

        private void SetSkyboxTexture(Texture value)
        {
            if (skyboxMaterial == null)
            {
                skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
                skyboxMaterial.DisableKeyword(LAT_LON);
                skyboxMaterial.EnableKeyword(SIDES_6);

                skyboxMaterial.SetInt("_Mapping", 0);
                skyboxMaterial.SetInt("_ImageType", 0);
                skyboxMaterial.SetInt("_MirrorOnBack", 0);
                skyboxMaterial.SetInt("_Layout", 0);
                RenderSettings.skybox = skyboxMaterial;
            }

            skyboxMaterial.SetTexture("_MainTex", value);
        }

        private void UpdateSkyBox()
        {
            skyboxMaterial.SetColor("_Tint", tint);
            skyboxMaterial.SetFloat("_Exposure", exposure);
            skyboxMaterial.SetFloat("_Rotation", rotation);
        }

        public void SetLatLngLocation(LatLngPoint location)
        {
            LatLngLocation = location;
            Location = LatLngLocation.ToCSV();

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