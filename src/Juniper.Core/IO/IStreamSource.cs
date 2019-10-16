using System.IO;
using System.Net;
using System.Threading.Tasks;

using Juniper.Progress;

namespace Juniper.IO
{
    public interface IContentReference
    {
        string CacheID { get; }

        MediaType ContentType { get; }
    }

    public interface IStreamSource : IContentReference
    {
        Task<Stream> GetStream(IProgress prog);
    }

    public static class IStreamSourceExt
    {
        public static async Task<ResultT> Decode<ResultT, MediaTypeT>(this IStreamSource source, IDeserializer<ResultT, MediaTypeT> deserializer, IProgress prog)
            where MediaTypeT : MediaType
        {
            prog.Report(0);
            var progs = prog.Split("Read", "Decode");
            var stream = await source.GetStream(progs[0]);
            var value = deserializer.Deserialize(stream, progs[1]);
            prog.Report(1);
            return value;
        }

        public static Task<ResultT> Decode<ResultT, MediaTypeT>(this IStreamSource source, IDeserializer<ResultT, MediaTypeT> deserializer)
            where MediaTypeT : MediaType
        {
            return Decode(source, deserializer, null);
        }

        public static Task<Stream> GetStream(this IStreamSource source)
        {
            return source.GetStream(null);
        }

        public static async Task Proxy(this IStreamSource source, HttpListenerResponse response)
        {
            var stream = await source.GetStream();
            response.ContentType = source.ContentType;
            await stream.Proxy(response);
        }

        public static Task Proxy<MediaTypeT>(this IStreamSource source, HttpListenerContext context)
            where MediaTypeT : MediaType
        {
            return source.Proxy(context.Response);
        }
    }
}
