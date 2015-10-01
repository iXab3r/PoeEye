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
    }
}