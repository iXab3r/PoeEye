using System;
using System.Linq;
using PoeEye.ItemParser.Services;
using PoeEye.PoeTrade.ViewModels;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class PoeTradeQuickFilter : IPoeTradeQuickFilter
    {
        private readonly IPoeItemSerializer serializer;

        public PoeTradeQuickFilter(IPoeItemSerializer serializer)
        {
            this.serializer = serializer;
        }

        public bool Apply(string text, IPoeTradeViewModel trade)
        {
            if (trade == null || trade.Trade == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            var result = false;

            var tokens = text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            var hashstack = new object[]
                {
                    trade.PriceInChaosOrbs,
                    trade.TradeState,
                    serializer.Serialize(trade.Trade)
                }.Where(x => x != null)
                 .Select(x => x.ToString()).ToArray();

            result |= tokens.All(token => hashstack.Any(entry => entry.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0));

            return result;
        }
    }
}