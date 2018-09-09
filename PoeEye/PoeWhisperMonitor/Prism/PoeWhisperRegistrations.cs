using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;
using Unity.Extension;

namespace PoeWhisperMonitor.Prism
{
    using Unity; using Unity.Resolution; using Unity.Attributes;

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