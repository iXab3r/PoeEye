namespace PoeShared.Common
{
    public class PoeLinksInfo : IPoeLinksInfo
    {
        public string RawSockets { get; set; }

        public override string ToString()
        {
            return $"Links: {RawSockets}";
        }
    }
}