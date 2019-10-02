using System.IO;
using System.Threading.Tasks;

using Juniper.HTTP;
using Juniper.IO;
using Juniper.Progress;

namespace Juniper.Compression.Zip
{
    public class ZipFileReference<MediaTypeT> : IStreamSource<MediaTypeT>
        where MediaTypeT : MediaType
    {
        private readonly ZipFileCacheLayer layer;
        private readonly IContentReference<MediaTypeT> source;

        public ZipFileReference(ZipFileCacheLayer layer, IContentReference<MediaTypeT> source)
        {
            this.layer = layer;
            this.source = source;
        }

        public string CacheID
        {
            get
            {
                return source.CacheID;
            }
        }

        public MediaTypeT ContentType
        {
            get
            {
                return source.ContentType;
            }
        }

        public Task<Stream> GetStream(IProgress prog)
        {
            Stream stream = null;
            var cacheFileName = layer.GetCacheFileName(source);
            var zip = Decompressor.OpenZip(layer.zipFile);
            var entry = zip.GetEntry(cacheFileName);
            if (entry != null)
            {
                stream = new ZipFileEntryStream(zip, entry, prog);
                if (prog != null)
                {
                    var length = entry.Size;
                    stream = new ProgressStream(stream, length, prog);
                }
            }
            return Task.FromResult(stream);
        }
    }
}
