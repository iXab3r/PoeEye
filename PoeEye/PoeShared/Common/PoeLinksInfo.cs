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
    }
}