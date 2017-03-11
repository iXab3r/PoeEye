using System.Text.RegularExpressions;
using Guards;
using JetBrains.Annotations;
using PoeShared.Converters;
using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Models.Parsers
{
    internal class PoeMessageStrictParserPoeTrade : PoeMessageParserRegex
    {
        private static readonly Regex MessageParser = new Regex(
            @"buy your (?'item'.+?)( listed for (?'price'.+?))? in (?'league'.*?) \(stash tab ""(?'tabName'.+?)""; position: left (?'itemX'\d+), top (?'itemY'\d+)\)[ ,\. ]*(?'offer'.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoeMessageStrictParserPoeTrade([NotNull] PriceToCurrencyConverter priceConverter) : base(priceConverter)
        {
        }

        protected override Regex GetMessageParser()
        {
            return MessageParser;
        }
    }

    internal class PoeMessageWeakParserPoeTrade : PoeMessageParserRegex
    {
        private static readonly Regex MessageParser = new Regex(
            @"buy your (?'item'.+?) listed for (?'price'.+?) in (?'league'.*?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoeMessageWeakParserPoeTrade([NotNull] PriceToCurrencyConverter priceConverter) : base(priceConverter)
        {
        }

        protected override Regex GetMessageParser()
        {
            return MessageParser;
        }
    }

    internal class PoeMessageCurrencyParserPoeTrade : PoeMessageParserRegex
    {
        private static readonly Regex MessageParser = new Regex(
            @"buy your (?'item'.+?) for my (?'price'.+?) in (?'league'.*?)?\.",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoeMessageCurrencyParserPoeTrade([NotNull] PriceToCurrencyConverter priceConverter) : base(priceConverter)
        {
        }

        protected override Regex GetMessageParser()
        {
            return MessageParser;
        }
    }
}