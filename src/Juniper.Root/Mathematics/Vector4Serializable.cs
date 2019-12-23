using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Juniper.Mathematics
{
    [Serializable]
    public struct Vector4Serializable : ISerializable
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float W;

        public Vector4Serializable(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Parameter `context` is required by ISerializable interface")]
        private Vector4Serializable(SerializationInfo info, StreamingContext context)
        {
            X = info.GetSingle(nameof(X));
            Y = info.GetSingle(nameof(Y));
            Z = info.GetSingle(nameof(Z));
            W = info.GetSingle(nameof(W));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(X), X);
            info.AddValue(nameof(Y), Y);
            info.AddValue(nameof(Z), Z);
            info.AddValue(nameof(W), W);
        }

        public override string ToString()
        {
            return $"<{X.ToString(CultureInfo.CurrentCulture)}, {Y.ToString(CultureInfo.CurrentCulture)}, {Z.ToString(CultureInfo.CurrentCulture)}, {W.ToString(CultureInfo.CurrentCulture)}>";
        }

        public override int GetHashCode()
        {
            return X.GetHashCode()
                ^ Y.GetHashCode()
                ^ Z.GetHashCode()
                ^ W.GetHashCode();
        }
    }
}
