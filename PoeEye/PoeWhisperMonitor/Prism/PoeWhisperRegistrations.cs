using PoeWhisperMonitor.Chat;
using Unity;
using Unity.Extension;

namespace PoeWhisperMonitor.Prism
{
    internal sealed class PoeWhisperRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeChatMessageProcessor, PoeChatMessageProcessor>()
                .RegisterType<IPoeMessagesSource, PoeMessagesSource>();

            Container
                .RegisterSingleton<IPoeTracker, PoeTracker>()
                .RegisterSingleton<IPoeChatService, PoeChatService>()
                .RegisterSingleton<IPoeWhisperService, PoeWhisperService>();
        }
    }
}