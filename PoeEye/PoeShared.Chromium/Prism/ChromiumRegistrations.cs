using PoeShared.Chromium.Communications;
using Unity;
using Unity.Extension;
using IChromiumBootstrapper = PoeShared.Chromium.Communications.IChromiumBootstrapper;
using IChromiumBrowserFactory = PoeShared.Chromium.Communications.IChromiumBrowserFactory;

namespace PoeShared.Chromium.Prism
{
    public sealed class ChromiumRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IChromiumBrowserFactory, ChromiumBrowserFactory>()
                .RegisterSingleton<IChromiumBootstrapper, ChromiumBootstrapper>();
        }
    }
}