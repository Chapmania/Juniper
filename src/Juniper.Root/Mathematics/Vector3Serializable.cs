using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Juniper.Mathematics
{
    [Serializable]
    public struct Vector3Serializable :
        ISerializable,
        IEquatable<Vector3Serializable>
    {
        private const string TYPE_NAME = "Vector3";

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public Vector3Serializable(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Parameter `context` is required by ISerializable interface")]
        private Vector3Serializable(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.CheckForType(TYPE_NAME);
            X = info.GetSingle(nameof(X));
            Y = info.GetSingle(nameof(Y));
            Z = info.GetSingle(nameof(Z));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("Type", TYPE_NAME);
            info.AddValue(nameof(X), X);
            info.AddValue(nameof(Y), Y);
            info.AddValue(nameof(Z), Z);
        }

        public override string ToString()
        {
            return $"<{X.ToString(CultureInfo.CurrentCulture)}, {Y.ToString(CultureInfo.CurrentCulture)}, {Z.ToString(CultureInfo.CurrentCulture)}>";
        }

        public override bool Equals(object obj)
        {
            return obj is Vector3Serializable serializable && Equals(serializable);
        }

        public bool Equals(Vector3Serializable other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            var hashCode = -307843816;
            hashCode = (hashCode * -1521134295) + X.GetHashCode();
            hashCode = (hashCode * -1521134295) + Y.GetHashCode();
            hashCode = (hashCode * -1521134295) + Z.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Vector3Serializable left, Vector3Serializable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3Serializable left, Vector3Serializable right)
        {
            return !(left == right);
        }

        public System.Numerics.Vector3 ToSystemVector3()
        {
            return new System.Numerics.Vector3(X, Y, Z);
        }

        public Accord.Math.Vector3 ToAccordVector3()
        {
            return new Accord.Math.Vector3(X, Y, Z);
        }

        public Accord.Math.Point3 ToAccordPoint3()
        {
            return new Accord.Math.Point3(X, Y, Z);
        }

        public static implicit operator System.Numerics.Vector3(Vector3Serializable v)
        {
            return v.ToSystemVector3();
        }

        public static implicit operator Vector3Serializable(System.Numerics.Vector3 v)
        {
            return System.Numerics.MathExt.ToJuniperVector3Serializable(v);
        }

        public static implicit operator Accord.Math.Vector3(Vector3Serializable v)
        {
            return v.ToAccordVector3();
        }

        public static implicit operator Vector3Serializable(Accord.Math.Vector3 v)
        {
            return Accord.Math.MathExt.ToJuniperVector3Serializable(v);
        }

        public static implicit operator Accord.Math.Point3(Vector3Serializable v)
        {
            return v.ToAccordPoint3();
        }

        public static implicit operator Vector3Serializable(Accord.Math.Point3 v)
        {
            return Accord.Math.MathExt.ToJuniperVector3Serializable(v);
        }
    }
}
