namespace PoeEye.Prism
{
    using Factory;

    using Microsoft.Practices.Unity;

    internal sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}