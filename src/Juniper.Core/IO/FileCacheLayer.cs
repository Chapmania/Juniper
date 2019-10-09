using System.IO;

namespace Juniper.IO
{
    public class FileCacheLayer : ICacheLayer
    {
        private readonly DirectoryInfo cacheLocation;

        public FileCacheLayer(DirectoryInfo cacheLocation)
        {
            this.cacheLocation = cacheLocation;
        }

        public FileCacheLayer(string directoryName)
            : this(new DirectoryInfo(directoryName))
        { }

        public virtual bool CanCache<MediaTypeT>(IContentReference<MediaTypeT> fileRef)
            where MediaTypeT : MediaType
        {
                return true;
        }

        protected virtual string GetCacheFileName<MediaTypeT>(IContentReference<MediaTypeT> fileRef)
            where MediaTypeT : MediaType
        {
            var baseName = fileRef.CacheID;
            var cacheFileName = fileRef.ContentType.AddExtension(baseName);
            return Path.Combine(cacheLocation.FullName, cacheFileName);
        }

        protected virtual FileInfo GetCacheFile<MediaTypeT>(IContentReference<MediaTypeT> fileRef)
            where MediaTypeT : MediaType
        {
            return new FileInfo(GetCacheFileName(fileRef));
        }

        public virtual Stream Cache<MediaTypeT>(IContentReference<MediaTypeT> fileRef, Stream stream)
            where MediaTypeT : MediaType
        {
            var cacheFile = GetCacheFile(fileRef);
            return new CachingStream(stream, cacheFile);
        }

        public Stream Create<MediaTypeT>(IContentReference<MediaTypeT> fileRef)
            where MediaTypeT : MediaType
        {
            var cacheFile = GetCacheFile(fileRef);
            cacheFile.Directory.Create();
            return cacheFile.Create();
        }

        public void Copy<MediaTypeT>(IContentReference<MediaTypeT> fileRef, FileInfo file)
            where MediaTypeT : MediaType
        {
            var cacheFile = GetCacheFile(fileRef);
            cacheFile.Directory.Create();
            File.Copy(file.FullName, cacheFile.FullName, true);
        }

        public bool IsCached<MediaTypeT>(IContentReference<MediaTypeT> fileRef)
            where MediaTypeT : MediaType
        {
            return GetCacheFile(fileRef).Exists;
        }

        public IStreamSource<MediaTypeT> GetCachedSource<MediaTypeT>(IContentReference<MediaTypeT> fileRef)
            where MediaTypeT : MediaType
        {
            return new FileReference<MediaTypeT>(GetCacheFile(fileRef), fileRef.CacheID, fileRef.ContentType);
        }
    }
}
