using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Juniper.HTTP;
using Juniper.Progress;

namespace Juniper.IO
{
    public static class ISerializerExt
    {
        public static void Serialize<T>(this ISerializer<T> serializer, HttpWebRequest request, MediaType type, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var stream = request.GetRequestStream();
            request.ContentType = type;
            request.ContentLength = serializer.Serialize(stream, value);
        }

        public static void Serialize<T>(this ISerializer<T> serializer, HttpListenerResponse response, MediaType type, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            response.ContentType = type;
            response.ContentLength64 = serializer.Serialize(response.OutputStream, value);
        }

        public static Task SerializeAsync<T>(this ISerializer<T> serializer, WebSocketConnection socket, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (socket is null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            var data = serializer.Serialize(value);
            return socket.SendAsync(data);
        }

        public static Task SerializeAsync<T>(this ISerializer<T> serializer, WebSocketConnection socket, string message, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (socket is null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return socket.SendAsync(message, value, serializer);
        }

        public static byte[] Serialize<T>(this ISerializer<T> serializer, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            using var mem = new MemoryStream();
            serializer.Serialize(mem, value);
            mem.Flush();

            return mem.ToArray();
        }

        public static void Serialize<T>(this ISerializer<T> serializer, FileInfo file, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using var stream = file.OpenRead();
            serializer.Serialize(stream, value);
        }

        public static void Serialize<T>(this ISerializer<T> serializer, string fileName, T value)
        {
            serializer.Serialize(new FileInfo(fileName.ValidateFileName()), value);
        }

        public static string ToString<T>(this ISerializer<T> serializer, T value)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            return Encoding.UTF8.GetString(serializer.Serialize(value));
        }
    }
}
