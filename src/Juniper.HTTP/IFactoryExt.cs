using System.Net;

using Juniper.Progress;

namespace Juniper.Serialization
{
    /// <summary>
    /// Perform HTTP queries
    /// </summary>
    public static class IFactoryExt
    {
        public static void Serialize<T>(this ISerializer serializer, HttpWebRequest request, T value)
        {
            using (var stream = request.GetRequestStream())
            {
                serializer.Serialize(stream, value);
            }
        }

        public static void Serialize<T>(this ISerializer serializer, HttpWebRequest request, T value, long length, IProgress progress)
        {
            using (var stream = request.GetRequestStream())
            {
                serializer.Serialize(stream, value, length, progress);
            }
        }

        public static void Serialize<T>(this ISerializer<T> serializer, HttpWebRequest request, T value)
        {
            using (var stream = request.GetRequestStream())
            {
                serializer.Serialize(stream, value);
            }
        }

        public static void Serialize<T>(this ISerializer<T> serializer, HttpWebRequest request, T value, long length, IProgress progress)
        {
            using (var stream = request.GetRequestStream())
            {
                serializer.Serialize(stream, value, length, progress);
            }
        }

        public static T Deserialize<T>(this IDeserializer deserializer, HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.Deserialize<T>(stream);
            }
        }

        public static T Deserialize<T>(this IDeserializer deserializer, HttpWebResponse response, IProgress progress)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.Deserialize<T>(stream, response.ContentLength, progress);
            }
        }

        public static bool TryDeserialize<T>(this IDeserializer deserializer, HttpWebResponse response, out T value)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.TryDeserialize(stream, out value);
            }
        }

        public static bool TryDeserialize<T>(this IDeserializer deserializer, HttpWebResponse response, out T value, IProgress progress)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.TryDeserialize(stream, out value, response.ContentLength, progress);
            }
        }

        public static T Deserialize<T>(this IDeserializer<T> deserializer, HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.Deserialize(stream);
            }
        }

        public static T Deserialize<T>(this IDeserializer<T> deserializer, HttpWebResponse response, IProgress progress)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.Deserialize(stream, response.ContentLength, progress);
            }
        }

        public static bool TryDeserialize<T>(this IDeserializer<T> deserializer, HttpWebResponse response, out T value)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.TryDeserialize(stream, out value);
            }
        }

        public static bool TryDeserialize<T>(this IDeserializer<T> deserializer, HttpWebResponse response, out T value, IProgress progress)
        {
            using (var stream = response.GetResponseStream())
            {
                return deserializer.TryDeserialize(stream, out value, response.ContentLength, progress);
            }
        }
    }
}