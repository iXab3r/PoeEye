using System;
using System.Text.RegularExpressions;
using Guards;

namespace PoeWhisperMonitor.Chat
{
    internal sealed class PoeChatMessageProcessor : IPoeChatMessageProcessor
    {
        private readonly Regex logRecordRegex = new Regex(@"^(?'timestamp'\d\d\d\d\/\d\d\/\d\d \d\d:\d\d:\d\d)( \w+ \w+ \[.*?\] :? ?)?(?'content'.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex messageParseRegex = new Regex(
            @"^(?'timestamp'\d\d\d\d\/\d\d\/\d\d \d\d:\d\d:\d\d).*?(?'prefix'[$&%]|@From|@To)\s?(?:\<(?'guild'.*?)\> )?(?'name'.*?):\s*(?'message'.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool TryParse(string rawText, out PoeMessage message)
        {
            Guard.ArgumentNotNull(rawText, nameof(rawText));

            message = default(PoeMessage);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }


            var logRecordMatch = logRecordRegex.Match(rawText);
            if (!logRecordMatch.Success)
            {
                return false;
            }

            var match = messageParseRegex.Match(rawText);
            if (!match.Success)
            {
                // system message, error, etc
                message = new PoeMessage
                {
                    Message = logRecordMatch.Groups["content"].Value,
                    MessageType = PoeMessageType.System,
                    Timestamp = DateTime.Parse(logRecordMatch.Groups["timestamp"].Value)
                };
                return true;
            }

            message = new PoeMessage
            {
                Message = match.Groups["message"].Value,
                MessageType = ToMessageType(match.Groups["prefix"].Value),
                Name = match.Groups["name"].Value,
                Timestamp = DateTime.Parse(match.Groups["timestamp"].Value)
            };

            return true;
        }

        private PoeMessageType ToMessageType(string prefix)
        {
            switch (prefix)
            {
                case "@From":
                    return PoeMessageType.WhisperIncoming;
                case "@To":
                    return PoeMessageType.WhisperOutgoing;
                case "$":
                    return PoeMessageType.Trade;
                case "%":
                    return PoeMessageType.Party;
                case "&":
                    return PoeMessageType.Guild;
                case "":
                    return PoeMessageType.Local;
                default:
                    return PoeMessageType.Unknown;
            }
        }
    }
}