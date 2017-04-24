using JetBrains.Annotations;

namespace PoeWhisperMonitor.Chat
{
    internal interface IPoeChatMessageProcessor
    {
        bool TryParse([NotNull] string rawText, out PoeMessage message);
    }
}