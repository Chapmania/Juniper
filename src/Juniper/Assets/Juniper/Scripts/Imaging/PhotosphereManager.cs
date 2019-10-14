﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Juniper.IO;

using UnityEngine;

namespace Juniper.Imaging
{
    public class PhotosphereManager : MonoBehaviour
    {
        private static Vector2[] MakeTestAngles(float fov, bool allAngles)
        {
            var list = new List<Vector2>();

            for (var ay = -180f; ay < 180; ay += fov)
            {
                for (var ax = -90f; ax <= 90f; ax += fov)
                {
                    if (Mathf.Abs(ax) != 90 || ay == 0 || allAngles)
                    {
                        list.Add(new Vector2(ay, ax));
                    }
                }
            }

            return list
                .Distinct()
                .OrderBy(x => x.magnitude)
                .ToArray();
        }

        internal int[] FOVs;
        internal Vector2[][] fovTestAngles;
        internal int[] lodLevelRequirements;

        private readonly Dictionary<string, Photosphere> photospheres = new Dictionary<string, Photosphere>();

        private Photosphere curSphere;

        public event CubemapImageNeeded CubemapNeeded;

        public event PhotosphereImageNeeded ImageNeeded;

        public event Action<Photosphere, bool> PhotosphereComplete;

        public event Action<Photosphere> PhotosphereReady;

        private CachingStrategy cache;
        private IImageCodec<Texture2D> codec;

        public void SetDetailLevels(float[] fovs)
        {
            FOVs = new int[fovs.Length];
            fovTestAngles = new Vector2[fovs.Length][];
            var requiredAngles = new Vector2[fovs.Length][];
            for (int f = 0; f < fovs.Length; ++f)
            {
                FOVs[f] = (int)fovs[f];
                fovTestAngles[f] = MakeTestAngles(fovs[f], true);
                requiredAngles[f] = MakeTestAngles(fovs[f], false);
            }

            lodLevelRequirements = requiredAngles
                .Select(a => a.Length)
                .ToArray();
        }

        public int Count
        {
            get
            {
                return photospheres.Count;
            }
        }

        public T GetPhotosphere<T>(string key) where T : Photosphere
        {
            if (curSphere == null || curSphere.CubemapName != key)
            {
                if (!photospheres.ContainsKey(key))
                {
                    CreatePhotosphere<Photosphere>(key);
                }

                curSphere = photospheres[key];
            }

            return (T)curSphere;
        }

        private IImageCodec<Texture2D> Photo_DecoderNeeded(Photosphere source)
        {
            if(codec != null)
            {
                source.DecoderNeeded -= Photo_DecoderNeeded;
            }
            return codec;
        }

        private CachingStrategy Photo_CacheNeeded(Photosphere source)
        {
            if(cache != null)
            {
                source.CacheNeeded -= Photo_CacheNeeded;
            }
            return cache;
        }

        private string Photo_CubemapNeeded(Photosphere source)
        {
            return CubemapNeeded?.Invoke(source);
        }

        private Task<Texture2D> Photo_ImageNeeded(Photosphere source, int lodLevel, int heading, int pitch)
        {
            return ImageNeeded?.Invoke(source, lodLevel, heading, pitch);
        }

        private void Photo_Ready(Photosphere obj)
        {
            obj.Ready -= Photo_Ready;
            PhotosphereReady?.Invoke(obj);
        }

        private void Photo_Complete(Photosphere obj, bool captureCubemap)
        {
            obj.CubemapNeeded -= Photo_CubemapNeeded;
            obj.ImageNeeded -= Photo_ImageNeeded;
            obj.Complete -= Photo_Complete;
            PhotosphereComplete?.Invoke(obj, captureCubemap);
        }

        private void CreatePhotosphere<T>(string key)
            where T : Photosphere
        {
            var photoGo = new GameObject(key);
            photoGo.Deactivate();
            photoGo.transform.SetParent(transform, true);

            var photo = photoGo.Ensure<T>().Value;
            photo.CubemapName = key;
            Initialize(photo);
        }

        public void SetIO(CachingStrategy cache, IImageCodec<Texture2D> codec)
        {
            this.cache = cache;
            this.codec = codec;
            var existing = GetComponentsInChildren<Photosphere>();
            foreach (var photo in existing)
            {
                photo.Deactivate();
                Initialize(photo);
            }
        }

        private void Initialize<T>(T photo)
            where T : Photosphere
        {
            photo.enabled = false;
            photo.CacheNeeded += Photo_CacheNeeded;
            photo.DecoderNeeded += Photo_DecoderNeeded;
            photo.Ready += Photo_Ready;
            photo.Complete += Photo_Complete;
            photo.CubemapNeeded += Photo_CubemapNeeded;
            photo.ImageNeeded += Photo_ImageNeeded;
            photospheres.Add(photo.CubemapName, photo);
        }
    }
}