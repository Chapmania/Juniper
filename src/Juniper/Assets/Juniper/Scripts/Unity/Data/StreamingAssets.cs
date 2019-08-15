using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Juniper.HTTP;
using Juniper.Progress;
using Juniper.Streams;

namespace Juniper.Data
{
    /// <summary>
    /// Get files out of the Unity StreamingAssets folder.
    /// </summary>
    public static class StreamingAssets
    {
        public static TimeSpan DEFAULT_TTL = TimeSpan.Zero;

        public static string FormatPath(string streamingAssetsPath, string subPath)
        {
            var parts = streamingAssetsPath.Split('/')
                .Union(subPath.Split('/'))
                .ToArray();
            var pathSep = '/';
            if (!NetworkPathPattern.IsMatch(streamingAssetsPath))
            {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_IOS
                pathSep = Path.DirectorySeparatorChar;
#elif UNITY_WEBGL
                UnityEngine.Debug.Log(streamingAssetsPath);
#endif
            }

            var path = parts.Join(pathSep);

            UnityEngine.Debug.Log(path);
            return path;
        }

        /// <summary>
        /// Open a file as a stream of bytes and save it to a cached location. On subsequent loads,
        /// open from the cached location.
        /// </summary>
        /// <param name="path">The full path to the file in question</param>
        /// <param name="ttl">The maximum age after which to consider a cached file invalidated.</param>
        /// <param name="mime">The mime type of the file, in case we have to request the file over the 'net.</param>
        /// <returns>Progress tracking object</returns>
        public static async Task<Response> GetStream(string cacheDirectory, string path, TimeSpan ttl, string mime, IProgress prog = null)
        {
            if (NetworkPathPattern.IsMatch(path))
            {
                var uri = new Uri(path);
                var cachePath = Uri.EscapeUriString(Path.Combine(cacheDirectory, uri.PathAndQuery));
                if (FileIsGood(cachePath, ttl))
                {
                    return new Response(mime, cachePath);
                }
                else
                {
                    var requester = HttpWebRequestExt.Create(uri).Accept(mime);
                    return new Response(await requester.Get());
                }
            }
#if UNITY_ANDROID
            else if (AndroidJarPattern.IsMatch(path))
            {
                var match = AndroidJarPattern.Match(path);
                var apk = match.Groups[1].Value;
                path = match.Groups[2].Value;
                var cachePath = Uri.EscapeUriString(Path.Combine(cacheDirectory, path));
                if (FileIsGood(cachePath, ttl))
                {
                    return new Response(mime, cachePath);
                }
                else
                {
                    var stream = Compression.Zip.Decompressor.GetFile(apk, path, prog);
                    return new Response(mime, new CachingStream(stream, cachePath));
                }
            }
#endif
            else if (File.Exists(path))
            {
                return new Response(mime, path);
            }
            else
            {
                return null;
            }
        }

        public static Task<Response> GetStream(string cacheDirectory, string path, TimeSpan ttl, IProgress prog = null)
        {
            return GetStream(cacheDirectory, path, ttl, "application/octet-stream", prog);
        }

        public static Task<Response> GetStream(string cacheDirectory, string path, string mime, IProgress prog = null)
        {
            return GetStream(cacheDirectory, path, DEFAULT_TTL, mime, prog);
        }

        public static Task<Response> GetStream(string cacheDirectory, string path, IProgress prog = null)
        {
            return GetStream(cacheDirectory, path, DEFAULT_TTL, "application/octet-stream", prog);
        }

        /// <summary>
        /// Parse out the network path.
        /// </summary>
        private const string NetworkPathPatternStr = "^https?://";

        /// <summary>
        /// Parse out the network path.
        /// </summary>
        private static readonly Regex NetworkPathPattern = new Regex(NetworkPathPatternStr, RegexOptions.Compiled);

#if UNITY_ANDROID

        /// <summary>
        /// The pattern to parse out the APK sub-file reference.
        /// </summary>
        private const string AndroidJarPatternStr = @"^jar:file:/([^!]+\.apk)!/(.+)$";

        /// <summary>
        /// The pattern to parse out the APK sub-file reference.
        /// </summary>
        private static readonly Regex AndroidJarPattern = new Regex(AndroidJarPatternStr, RegexOptions.Compiled);

#endif

        private static bool FileIsGood(string path, TimeSpan ttl)
        {
            return File.Exists(path) && File.GetCreationTime(path) - DateTime.Now <= ttl;
        }
    }
}