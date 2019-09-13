using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Juniper.Google.Maps.Geocoding
{
    [Serializable]
    public class AddressComponent : ISerializable
    {
        public static int HashAddressComponents(IEnumerable<AddressComponentType> types)
        {
            var key = 0;
            foreach (var type in types)
            {
                key ^= type.GetHashCode();
            }
            return key;
        }

        public readonly string long_name;
        public readonly string short_name;
        public readonly string[] typeStrings;
        public readonly HashSet<AddressComponentType> types;

        internal int Key { get; }

        protected AddressComponent(SerializationInfo info, StreamingContext context)
        {
            long_name = info.GetString(nameof(long_name));
            short_name = info.GetString(nameof(short_name));
            typeStrings = info.GetValue<string[]>(nameof(types));
            types = new HashSet<AddressComponentType>(from typeStr in typeStrings
                                                      select Enum.TryParse<AddressComponentType>(typeStr, out var parsedType)
                                                          ? parsedType
                                                          : AddressComponentType.Unknown);

            Key = HashAddressComponents(types);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(long_name), long_name);
            info.AddValue(nameof(short_name), short_name);
            info.AddValue(nameof(types), typeStrings);
        }

        public override int GetHashCode()
        {
            return long_name.GetHashCode() ^ Key;
        }

        public override bool Equals(object obj)
        {
            return obj is AddressComponent addr && this == addr;
        }

        public static bool operator ==(AddressComponent left, AddressComponent right)
        {
            return ReferenceEquals(left, right)
                || !ReferenceEquals(left, null)
                    && !ReferenceEquals(right, null)
                    && left.Key == right.Key
                    && left.long_name == right.long_name;
        }

        public static bool operator !=(AddressComponent left, AddressComponent right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            var t = typeStrings.ToString("|");

            return $"{t}:{long_name}";
        }
    }
}