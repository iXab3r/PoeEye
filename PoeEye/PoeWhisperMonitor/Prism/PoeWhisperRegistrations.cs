namespace PoeWhisperMonitor.Prism
{
    using Microsoft.Practices.Unity;

    public sealed class PoeWhisperRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeWhispers, PoeWhispers>();
        }
    }
}