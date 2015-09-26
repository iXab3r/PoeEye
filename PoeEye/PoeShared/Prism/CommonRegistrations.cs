namespace PoeShared.Prism
{
    using Factory;

    using Microsoft.Practices.Unity;

    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}