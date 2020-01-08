using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Juniper.World.GIS.Google.Geocoding
{
    [Serializable]
    public sealed class AddressComponent : ISerializable, IEquatable<AddressComponent>
    {
        private static readonly string LONG_NAME_FIELD = nameof(Long_Name).ToLowerInvariant();
        private static readonly string SHORT_NAME_FIELD = nameof(Short_Name).ToLowerInvariant();
        private static readonly string TYPES_FIELD = nameof(Types).ToLowerInvariant();

        public static int HashAddressComponents(IEnumerable<AddressComponentTypes> types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var key = 0;
            foreach (var type in types)
            {
                key ^= type.GetHashCode();
            }

            return key;
        }

        public string Long_Name { get; }

        public string Short_Name { get; }

        public string[] TypeStrings { get; }

        public HashSet<AddressComponentTypes> Types { get; }

        internal int Key { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Parameter `context` is required by ISerializable interface")]
        private AddressComponent(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Long_Name = info.GetString(LONG_NAME_FIELD);
            Short_Name = info.GetString(SHORT_NAME_FIELD);
            TypeStrings = info.GetValue<string[]>(TYPES_FIELD);
            Types = new HashSet<AddressComponentTypes>(
                from typeStr in TypeStrings
                select Enum.TryParse<AddressComponentTypes>(typeStr, out var parsedType)
                    ? parsedType
                    : AddressComponentTypes.None);

            Key = HashAddressComponents(Types);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(LONG_NAME_FIELD, Long_Name);
            info.AddValue(SHORT_NAME_FIELD, Short_Name);
            info.AddValue(TYPES_FIELD, TypeStrings);
        }

        public override int GetHashCode()
        {
            return Long_Name.GetHashCode() ^ Key;
        }

        public override bool Equals(object obj)
        {
            return obj is AddressComponent addr && Equals(addr);
        }

        public bool Equals(AddressComponent other)
        {
            return other is object
                && Key == other.Key
                && Long_Name == other.Long_Name;
        }

        public static bool operator ==(AddressComponent left, AddressComponent right)
        {
            return ReferenceEquals(left, right)
                || (left is object && left.Equals(right));
        }

        public static bool operator !=(AddressComponent left, AddressComponent right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            var t = TypeStrings.ToString("|");

            return $"{t}:{Long_Name}";
        }
    }
}