using System.IO;
using System.Threading.Tasks;

using Juniper.Compression.Zip;
using Juniper.Progress;

using UnityEngine;

namespace Juniper.IO
{
    public class StreamingAssetsCacheLayer : FileCacheLayer
    {
        public static bool NeedsUnpack
        {
            get
            {
                return Application.platform == RuntimePlatform.Android
                    && !Application.isEditor;
            }
        }

        public static Task UnpackAPKAsync(IProgress prog)
        {
            var apk = Application.dataPath;
            var appData = Application.persistentDataPath;
            return Task.Run(() =>
                Decompressor.Decompress(apk, appData, "assets/", prog));
        }

        private static string RootDir =>
            PathExt.FixPath(NeedsUnpack
                ? Application.persistentDataPath
                : Application.streamingAssetsPath);

        public StreamingAssetsCacheLayer()
            : base(RootDir)
        { }

        public StreamingAssetsCacheLayer(string subDirName)
            : base(Path.Combine(RootDir, subDirName))
        { }
    }
}