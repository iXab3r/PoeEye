using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;

namespace PoeWhisperMonitor.Prism
{
    using Microsoft.Practices.Unity;

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