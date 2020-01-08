using System;
using System.Runtime.Serialization;

namespace Juniper.Mathematics
{
    [Serializable]
    public struct Matrix3x2Serializable : ISerializable
    {
        private static readonly string VALUES_FIELD = nameof(Values).ToLowerInvariant();

        public float[] Values { get; }

        public Matrix3x2Serializable(float m11, float m12, float m21, float m22, float m31, float m32)
        {
            Values = new[]
            {
                m11, m12,
                m21, m22,
                m31, m32
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Parameter `context` is required by ISerializable interface")]
        private Matrix3x2Serializable(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Values = info.GetValue<float[]>(VALUES_FIELD);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(VALUES_FIELD, Values);
        }

        //public static implicit operator Matrix3x2(Matrix3x2Serializable v)
        //{
        //    return new Matrix3x2(
        //        v.values[0], v.values[1],
        //        v.values[2], v.values[3],
        //        v.values[4], v.values[5]);
        //}

        //public static explicit operator Matrix3x2Serializable(Matrix3x2 v)
        //{
        //    return new Matrix3x2Serializable(
        //        v.M11, v.M12,
        //        v.M21, v.M22,
        //        v.M31, v.M32);
        //}
    }
}
