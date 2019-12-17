using System.IO;
using System.Net;
using System.Threading.Tasks;

using Juniper.Progress;

namespace Juniper.IO
{
    public abstract class StreamSource : ContentReference
    {
        protected StreamSource(MediaType contentType)
            : base(contentType)
        { }

        protected StreamSource(string cacheID, MediaType contentType)
            : base(cacheID, contentType)
        { }

        public abstract Task<Stream> GetStream(IProgress prog);
    }

    public static class StreamSourceExt
    {
        public static async Task<ResultT> Decode<ResultT>(this StreamSource source, IDeserializer<ResultT> deserializer, IProgress prog = null)
        {
            prog.Report(0);
            var progs = prog.Split("Read", "Decode");
            var stream = await source
                .GetStream(progs[0])
                .ConfigureAwait(false);
            var value = deserializer.Deserialize(stream, progs[1]);
            prog.Report(1);
            return value;
        }

        public static Task<Stream> GetStream(this StreamSource source)
        {
            return source.GetStream(null);
        }

        public static async Task Proxy(this StreamSource source, HttpListenerResponse response)
        {
            var stream = await source
                .GetStream()
                .ConfigureAwait(false);
            response.ContentType = source.ContentType;
            await stream
                .Proxy(response)
                .ConfigureAwait(false);
        }

        public static Task Proxy(this StreamSource source, HttpListenerContext context)
        {
            return source.Proxy(context.Response);
        }
    }
}
