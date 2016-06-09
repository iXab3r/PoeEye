using System;
using JetBrains.Annotations;

namespace PoeWhisperMonitor.Chat
{
    public interface IPoeChatService : IDisposable
    {
        PoeMessageSendStatus SendMessage([NotNull] string message);
    }
}