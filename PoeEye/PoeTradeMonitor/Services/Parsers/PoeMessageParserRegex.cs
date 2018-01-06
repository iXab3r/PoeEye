using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Services.Parsers
{
    internal abstract class PoeMessageParserRegex : IPoeMessageParser
    {
        private readonly StringToPoePriceConverter stringToPoePriceConverter;

        public PoeMessageParserRegex([NotNull] StringToPoePriceConverter stringToPoePriceConverter)
        {
            Guard.ArgumentNotNull(stringToPoePriceConverter, nameof(stringToPoePriceConverter));
            this.stringToPoePriceConverter = stringToPoePriceConverter;
        }

        protected abstract Regex GetMessageParser();

        public bool TryParse(PoeMessage message, out TradeModel result)
        {
            result = default(TradeModel);

            var match = GetMessageParser()?.Match(message.Message);
            if (match != null && match.Success)
            {
                try
                {
                    result = new TradeModel
                    {
                        CharacterName = message.Name,
                        PositionName = GetGroupOrDefault(match, "item"),
                        Price = stringToPoePriceConverter.Convert(GetGroupOrDefault(match, "price")),
                        League = GetGroupOrDefault(match, "league"),
                        TabName = GetGroupOrDefault(match, "tabName"),
                        ItemPosition = new ItemPosition
                        {
                            X = int.Parse(GetGroupOrDefault(match, "itemX", "1")) - 1,
                            Y = int.Parse(GetGroupOrDefault(match, "itemY", "1")) - 1,
                        },
                        Timestamp = message.Timestamp,
                        Offer = GetGroupOrDefault(match, "offer"),
                        TradeType = message.MessageType == PoeMessageType.WhisperIncoming 
                            ? TradeType.Sell 
                            : TradeType.Buy
                    };
                }
                catch (Exception e)
                {
                    throw new FormatException($"Failed to parse string '{message}', match\n{CollectMatchInfo(match)}", e);
                }
                
                return true;
            }

            return false;
        }

        private string GetGroupOrDefault(Match match, string groupName, string defaultValue = null)
        {
            return match.Success && match.Groups[groupName].Success 
                ? match.Groups[groupName].Value 
                : defaultValue;
        }

        private string CollectMatchInfo(Match match)
        {
            var groups = new List<string>();
            foreach (Group matchGroup in match.Groups)
            {
                groups.Add(matchGroup.Value);
            }

            return new
            {
                Success = match.Success,
                Value = match.Value,
                Groups = groups
            }.DumpToText();
        }
    }
}
