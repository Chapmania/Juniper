using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using Juniper.Google;

namespace Juniper.World.GIS.Google.StreetView
{
    [Serializable]
    public class MetadataResponse :
        ISerializable,
        IEquatable<MetadataResponse>,
        IComparable<MetadataResponse>
    {
        private static readonly Regex PANO_PATTERN = new Regex("^[a-zA-Z0-9_\\-]+$", RegexOptions.Compiled);

        public static bool IsPano(string panoString)
        {
            return PANO_PATTERN.IsMatch(panoString);
        }

        public HttpStatusCode Status { get; }

        public string Copyright { get; }

        public DateTime Date { get; }

        public string Pano_ID { get; }

        public LatLngPoint Location { get; }

        protected MetadataResponse(MetadataResponse copy)
        {
            if (copy is null)
            {
                throw new ArgumentNullException(nameof(copy));
            }

            Status = copy.Status;
            Copyright = copy.Copyright;
            Date = copy.Date;
            Pano_ID = copy.Pano_ID;
            Location = copy.Location;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Parameter `context` is required by ISerializable interface")]
        protected MetadataResponse(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            foreach(var field in info)
            {
                switch (field.Name.ToLowerInvariant())
                {
                    case "status": Status = info.GetString(field.Name).MapToStatusCode(); break;
                    case "copyright": Copyright = info.GetString(field.Name); break;
                    case "date": Date = info.GetDateTime(field.Name); break;
                    case "pano_id": Pano_ID = info.GetString(field.Name); break;
                    case "location":  Location = info.GetValue<LatLngPoint>(field.Name); break;
                }
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(Status), Status.ToString());
            if (Status == HttpStatusCode.OK)
            {
                _ = info.MaybeAddValue(nameof(Copyright), Copyright);
                _ = info.MaybeAddValue(nameof(Date), Date.ToString("yyyy-MM", CultureInfo.InvariantCulture));
                _ = info.MaybeAddValue(nameof(Pano_ID), Pano_ID);
                _ = info.MaybeAddValue(nameof(Location), new
                {
                    lat = Location.Latitude,
                    lng = Location.Longitude
                });
            }
        }

        public int CompareTo(MetadataResponse other)
        {
            if (other is null)
            {
                return -1;
            }
            else
            {
                var byPano = string.CompareOrdinal(Pano_ID, other.Pano_ID);
                var byLocation = Location.CompareTo(other.Location);
                var byDate = Date.CompareTo(other.Date);
                var byCopyright = string.CompareOrdinal(Copyright, other.Copyright);

                if (byPano == 0
                    && byLocation == 0
                    && byDate == 0)
                {
                    return byCopyright;
                }
                else if (byPano == 0
                    && byLocation == 0)
                {
                    return byDate;
                }
                else if (byPano == 0)
                {
                    return byLocation;
                }
                else
                {
                    return byPano;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataResponse other
                && Equals(other);
        }

        public bool Equals(MetadataResponse other)
        {
            return other is object
                && CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            var hashCode = -1311455165;
            hashCode = (hashCode * -1521134295) + Status.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Copyright);
            hashCode = (hashCode * -1521134295) + Date.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Pano_ID);
            hashCode = (hashCode * -1521134295) + EqualityComparer<LatLngPoint>.Default.GetHashCode(Location);
            return hashCode;
        }

        public static bool operator ==(MetadataResponse left, MetadataResponse right)
        {
            return (left is null && right is null)
                || (left is object && left.Equals(right));
        }

        public static bool operator !=(MetadataResponse left, MetadataResponse right)
        {
            return !(left == right);
        }

        public static bool operator <(MetadataResponse left, MetadataResponse right)
        {
            return left is null
                ? right is object
                : left.CompareTo(right) < 0;
        }

        public static bool operator <=(MetadataResponse left, MetadataResponse right)
        {
            return left is null
                || left.CompareTo(right) <= 0;
        }

        public static bool operator >(MetadataResponse left, MetadataResponse right)
        {
            return left is object
                && left.CompareTo(right) > 0;
        }

        public static bool operator >=(MetadataResponse left, MetadataResponse right)
        {
            return left is null
                ? right is null
                : left.CompareTo(right) >= 0;
        }
    }
}