using System;
using System.Runtime.Serialization;

namespace Juniper.Google.Maps
{
    [Serializable]
    public sealed class USAddress :
        ISerializable,
        IEquatable<USAddress>,
        IEquatable<string>
    {
        private readonly string street;
        private readonly string city;
        private readonly string state;
        private readonly string zip;

        public USAddress(string street, string city, string state, string zip)
        {
            this.street = street;
            this.city = city;
            this.state = state;
            this.zip = zip;
        }

        private USAddress(SerializationInfo info, StreamingContext context)
        {
            street = info.GetString(nameof(street));
            city = info.GetString(nameof(city));
            state = info.GetString(nameof(state));
            zip = info.GetString(nameof(zip));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(street), street);
            info.AddValue(nameof(city), city);
            info.AddValue(nameof(state), state);
            info.AddValue(nameof(zip), zip);
        }

        public USAddress(string street, string city, string state)
            : this(street, city, state, string.Empty) { }

        public override string ToString()
        {
            return $"{street}, {city}, {state}, {zip}";
        }

        public override bool Equals(object obj)
        {
            return obj is USAddress addr && Equals(addr)
                || obj is string name && Equals(name);
        }

        public bool Equals(USAddress other)
        {
            return other is object
                && street == other.street
                && city == other.city
                && state == other.state
                && zip == other.zip;
        }

        public bool Equals(string name)
        {
            return ToString() == name;
        }

        public static bool operator ==(USAddress left, USAddress right)
        {
            return ReferenceEquals(left, right)
                || left is object && left.Equals(right);
        }

        public static bool operator !=(USAddress left, USAddress right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return street.GetHashCode()
                ^ city.GetHashCode()
                ^ state.GetHashCode()
                ^ zip.GetHashCode();
        }
    }
}