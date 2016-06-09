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
                .RegisterSingleton<IPoeTracker, PoeTracker>()
                .RegisterSingleton<IPoeChatService, PoeChatService>()
                .RegisterSingleton<IPoeWhisperService, PoeWhisperService>();
        }
    }
}