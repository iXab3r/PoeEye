using System;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeLinksInfoViewModel : DisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeLinksInfoViewModel));

        private readonly string[] colors = new string[6];
        private readonly bool[] links = new bool[6];

        public PoeLinksInfoViewModel([NotNull] IPoeLinksInfo linksInfo)
        {
            Guard.ArgumentNotNull(linksInfo, nameof(linksInfo));

            var rawSockets = linksInfo.RawSockets ?? string.Empty;

            try
            {
                var socketIdx = 0;
                var linkIdx = 0;
                foreach (var symbol in rawSockets)
                {
                    if (symbol == '-')
                    {
                        links[linkIdx - 1] = true;
                        continue;
                    }

                    colors[socketIdx] = symbol.ToString();
                    socketIdx++;
                    linkIdx++;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to parse sockets {rawSockets}", e);
            }
        }

        public string Socket1 => colors[0];

        public string Socket2 => colors[1];

        public string Socket3 => colors[2];

        public string Socket4 => colors[3];

        public string Socket5 => colors[4];

        public string Socket6 => colors[5];

        public bool Link1 => links[0];

        public bool Link2 => links[1];

        public bool Link3 => links[2];

        public bool Link4 => links[3];

        public bool Link5 => links[4];
    }
}