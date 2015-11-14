namespace PoeShared.Common
{
    public class PoeLinksInfo : IPoeLinksInfo
    {
        public string RawSockets { get; set; }

        public PoeLinksInfo(string rawSockets)
        {
            RawSockets = rawSockets;
        }

        public PoeLinksInfo()
        {
        }

        public override string ToString()
        {
            return $"Links: {RawSockets}";
        }

        protected bool Equals(PoeLinksInfo other)
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
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((PoeLinksInfo) obj);
        }

        public override int GetHashCode()
        {
            return (RawSockets != null ? RawSockets.GetHashCode() : 0);
        }
    }
}