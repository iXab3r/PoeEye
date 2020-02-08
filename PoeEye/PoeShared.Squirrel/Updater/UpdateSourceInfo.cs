using System.Security;
using Newtonsoft.Json;
using PoeShared.Converters;

namespace PoeShared.Squirrel.Updater
{
    public struct UpdateSourceInfo
    {
        public string Uri { get; set; }

        public string Description { get; set; }

        public bool RequiresAuthentication { get; set; }

        [JsonConverter(typeof(SafeDataConverter))]
        public SecureString Username { get; set; }

        [JsonConverter(typeof(SafeDataConverter))]
        public SecureString Password { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(Uri);

        public bool Equals(UpdateSourceInfo other)
        {
            return string.Equals(Uri, other.Uri);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is UpdateSourceInfo && Equals((UpdateSourceInfo) obj);
        }

        public override int GetHashCode()
        {
            return Uri != null
                ? Uri.GetHashCode()
                : 0;
        }

        public static bool operator ==(UpdateSourceInfo left, UpdateSourceInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UpdateSourceInfo left, UpdateSourceInfo right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{nameof(Uri)}: {Uri}, {nameof(Description)}: {Description}";
        }
    }
}