using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Juniper.Mathematics
{
    [Serializable]
    public struct Vector4Serializable :
        ISerializable,
        IEquatable<Vector4Serializable>
    {
        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public float W { get; }

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
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            X = info.GetSingle(nameof(X));
            Y = info.GetSingle(nameof(Y));
            Z = info.GetSingle(nameof(Z));
            W = info.GetSingle(nameof(W));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(X), X);
            info.AddValue(nameof(Y), Y);
            info.AddValue(nameof(Z), Z);
            info.AddValue(nameof(W), W);
        }

        public override string ToString()
        {
            return $"<{X.ToString(CultureInfo.CurrentCulture)}, {Y.ToString(CultureInfo.CurrentCulture)}, {Z.ToString(CultureInfo.CurrentCulture)}, {W.ToString(CultureInfo.CurrentCulture)}>";
        }

        public override bool Equals(object obj)
        {
            return obj is Vector4Serializable serializable
                && Equals(serializable);
        }

        public bool Equals(Vector4Serializable serializable)
        {
            return X == serializable.X
                && Y == serializable.Y
                && Z == serializable.Z
                && W == serializable.W;
        }

        public override int GetHashCode()
        {
            var hashCode = 707706286;
            hashCode = (hashCode * -1521134295) + X.GetHashCode();
            hashCode = (hashCode * -1521134295) + Y.GetHashCode();
            hashCode = (hashCode * -1521134295) + Z.GetHashCode();
            hashCode = (hashCode * -1521134295) + W.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Vector4Serializable left, Vector4Serializable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4Serializable left, Vector4Serializable right)
        {
            return !(left == right);
        }

        public System.Numerics.Vector4 ToSystemVector4()
        {
            return new System.Numerics.Vector4(X, Y, Z, W);
        }

        public Accord.Math.Vector4 ToAccordVector4()
        {
            return new Accord.Math.Vector4(X, Y, Z, W);
        }

        public static implicit operator System.Numerics.Vector4(Vector4Serializable v)
        {
            return v.ToSystemVector4();
        }

        public static implicit operator Vector4Serializable(System.Numerics.Vector4 v)
        {
            return System.Numerics.MathExt.ToJuniperVector4Serializable(v);
        }

        public static implicit operator Accord.Math.Vector4(Vector4Serializable v)
        {
            return v.ToAccordVector4();
        }

        public static implicit operator Vector4Serializable(Accord.Math.Vector4 v)
        {
            return Accord.Math.MathExt.ToJuniperVector4Serializable(v);
        }
    }
}
