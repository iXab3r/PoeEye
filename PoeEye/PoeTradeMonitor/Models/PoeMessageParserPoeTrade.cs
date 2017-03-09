using System.Text.RegularExpressions;
using Guards;
using JetBrains.Annotations;
using PoeShared.Converters;
using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Models
{
    internal class PoeMessageParserPoeTrade : IPoeMessageParser
    {
        private readonly PriceToCurrencyConverter priceConverter;

        private static readonly Regex MessageParser = new Regex(
            "buy your (?'item'.*?) listed for (?'price'.*?) in",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PoeMessageParserPoeTrade([NotNull] PriceToCurrencyConverter priceConverter)
        {
            Guard.ArgumentNotNull(() => priceConverter);
            this.priceConverter = priceConverter;
        }

        public bool TryParse(PoeMessage message, out TradeModel result)
        {
            result = default(TradeModel);

            var match = MessageParser.Match(message.Message);
            if (match.Success)
            {
                result = new TradeModel
                {
                    CharacterName = message.Name,
                    ItemName = match.Groups["item"].Value,
                    Price = priceConverter.Convert(match.Groups["price"].Value),
                    Timestamp = message.Timestamp,
                };
                return true;
            }

            return false;
        }
    }
}