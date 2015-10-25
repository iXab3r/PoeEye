namespace PoeShared.Common
{
    using Guards;

    public sealed class PoeLinksInfo : IPoeLinksInfo
    {
        public PoeLinksInfo(string rawSockets)
        {
            Guard.ArgumentNotNullOrEmpty(() => rawSockets);

            RawSockets = rawSockets;
        }

        public string RawSockets { get; }

        private bool Equals(PoeLinksInfo other)
        {
            return string.Equals(RawSockets, other.RawSockets);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is PoeLinksInfo && Equals((PoeLinksInfo) obj);
        }

        public override int GetHashCode()
        {
            return RawSockets.GetHashCode();
        }

        public override string ToString()
        {
            return $"Links: {RawSockets}";
        }
    }
}