namespace PoeEye.PoeTrade.ViewModels
{
    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.Scaffolding;

    internal sealed class PoeLinksInfoViewModel : DisposableReactiveObject
    {
        public PoeLinksInfoViewModel([NotNull] IPoeLinksInfo linksInfo)
        {
            Guard.ArgumentNotNull(linksInfo, nameof(linksInfo));

            var rawSockets = linksInfo.RawSockets ?? string.Empty;
            rawSockets = rawSockets.PadRight(11, ' ');

            var parsedSockets = rawSockets.Replace(" ", string.Empty);
            parsedSockets = parsedSockets.Replace("-", string.Empty);

            var sockets = new string[6];
            for (var i = 0; i < parsedSockets.Length; i++)
            {
                sockets[i] = parsedSockets.Substring(i, 1);
            }

            Socket1 = sockets[0];
            Socket2 = sockets[1];
            Socket3 = sockets[2];
            Socket4 = sockets[3];
            Socket5 = sockets[4];
            Socket6 = sockets[5];

            Link1 = rawSockets[1] == '-';
            Link2 = rawSockets[3] == '-';
            Link3 = rawSockets[5] == '-';
            Link4 = rawSockets[7] == '-';
            Link5 = rawSockets[9] == '-';
        }


        public string Socket1 { get; }

        public string Socket2 { get; }

        public string Socket3 { get; }

        public string Socket4 { get; }

        public string Socket5 { get; }

        public string Socket6 { get; }

        public bool Link1 { get; }

        public bool Link2 { get; }

        public bool Link3 { get; }

        public bool Link4 { get; }

        public bool Link5 { get; }
    }
}
