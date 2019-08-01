using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Juniper.Serialization;

namespace Juniper.HTTP.REST
{
    public abstract class AbstractSingleRequest<ResponseType> : AbstractRequest<ResponseType, ResponseType>
    {
        private readonly Dictionary<string, List<string>> queryParams = new Dictionary<string, List<string>>();
        private readonly UriBuilder uriBuilder;
        private readonly string cacheLocString;
        protected IDeserializer<ResponseType> deserializer;
        private string acceptType;
        private string extension;

        protected AbstractSingleRequest(AbstractEndpoint api, Uri baseServiceURI, IDeserializer<ResponseType> deserializer, string path, string cacheLocString)
            : base(api)
        {
            uriBuilder = new UriBuilder(baseServiceURI);
            uriBuilder.Path += path;
            this.deserializer = deserializer;
            this.cacheLocString = cacheLocString;
        }

        protected void SetContentType(string acceptType, string extension)
        {
            this.acceptType = acceptType;
            this.extension = extension;
        }

        protected U SetQuery<U>(string key, U value)
        {
            SetQuery(key, value.ToString());
            return value;
        }

        protected void RemoveQuery(string key)
        {
            queryParams.Remove(key);
        }

        private string SetQuery(string key, string value, bool allowMany)
        {
            var list = queryParams.Get(key) ?? new List<string>();
            if (allowMany || list.Count == 0)
            {
                list.Add(value);
            }
            else if (!allowMany)
            {
                list[0] = value;
            }
            queryParams[key] = list;

            return value;
        }

        protected string SetQuery(string key, string value)
        {
            return SetQuery(key, value, false);
        }

        protected string AddQuery(string key, string value)
        {
            return SetQuery(key, value, true);
        }

        protected U AddQuery<U>(string key, U value)
        {
            SetQuery(key, value.ToString());
            return value;
        }

        public virtual Uri BaseURI
        {
            get
            {
                uriBuilder.Query = queryParams.ToString("=", "&");
                return uriBuilder.Uri;
            }
        }

        protected virtual Uri AuthenticatedURI
        {
            get
            {
                return BaseURI;
            }
        }

        public FileInfo CacheFile
        {
            get
            {
                return new FileInfo(CacheFileName);
            }
        }

        protected virtual string CacheFileName
        {
            get
            {
                var cacheID = string.Join("_", BaseURI.Query
                                .Substring(1)
                                .Split(Path.GetInvalidFileNameChars()));
                var path = Path.Combine(api.cacheLocation.FullName, cacheLocString, cacheID);
                if (!extension.StartsWith("."))
                {
                    path += ".";
                }
                path += extension;
                return path;
            }
        }

        public override bool IsCached
        {
            get
            {
                return CacheFile.Exists;
            }
        }

        private void SetAcceptType(HttpWebRequest request)
        {
            request.Accept = acceptType;
        }

        private Task<T> Get<T>(Func<Stream, T> decoder)
        {
            var uri = AuthenticatedURI;
            var file = CacheFile;
            return Task.Run(() => HttpWebRequestExt.CachedGet(uri, decoder, file, SetAcceptType));
        }

        public override Task<ResponseType> Get()
        {
            return Get(deserializer.Deserialize);
        }

        public Task<Stream> GetRaw()
        {
            return Get(stream => stream);
        }

        private Task<T> Post<T>(Func<Stream, T> decoder)
        {
            var uri = AuthenticatedURI;
            var file = CacheFile;
            return Task.Run(() => HttpWebRequestExt.CachedPost(uri, decoder, file, SetAcceptType));
        }

        public override Task<ResponseType> Post()
        {
            return Post(deserializer.Deserialize);
        }

        public Task<Stream> PostRaw()
        {
            return Post(stream => stream);
        }
    }
}