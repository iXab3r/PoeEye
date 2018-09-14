using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Converters;
using TypeConverter;

namespace PoeEye.TradeMonitor.Services.Parsers
{
    internal class PoeMessageStrictParserPoeTrade : PoeMessageParserRegex
    {
        private static readonly Regex MessageParser = new Regex(
            @"buy your (?'item'.+?)( listed for (?'price'.+?))? in (?'league'.*?) \(stash tab ""(?'tabName'.+?)""; position: left (?'itemX'\d+), top (?'itemY'\d+)\)[ ,\. ]*(?'offer'.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoeMessageStrictParserPoeTrade([NotNull] IConverter<string, PoePrice> stringToPoePriceConverter) : base(stringToPoePriceConverter)
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

        public PoeMessageWeakParserPoeTrade([NotNull] IConverter<string, PoePrice> stringToPoePriceConverter) : base(stringToPoePriceConverter)
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

        public PoeMessageCurrencyParserPoeTrade([NotNull] IConverter<string, PoePrice> stringToPoePriceConverter) : base(stringToPoePriceConverter)
        {
        }

        protected override Regex GetMessageParser()
        {
            return MessageParser;
        }
    }
}