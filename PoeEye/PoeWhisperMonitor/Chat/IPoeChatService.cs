using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeWhisperMonitor.Chat
{
    public interface IPoeChatService : IDisposableReactiveObject
    {
        bool IsAvailable { get; }

        bool IsBusy { get; }

        Task<PoeMessageSendStatus> SendMessage([NotNull] string message);

        Task<PoeMessageSendStatus> SendMessage([NotNull] string message, bool terminateByPressingEnter);
    }
}