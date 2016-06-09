using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeWhisperMonitor.Chat
{
    public interface IPoeChatService : IDisposableReactiveObject
    {
        bool IsAvailable { get; }

        PoeMessageSendStatus SendMessage([NotNull] string message);
    }
}