namespace PoeWhisperMonitor.Prism
{
    using Microsoft.Practices.Unity;

    internal sealed class PoeWhisperRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeWhisperService, PoeWhisperService>();
        }
    }
}