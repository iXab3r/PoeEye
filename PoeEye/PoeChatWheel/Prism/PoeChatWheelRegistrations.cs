using PoeChatWheel.ViewModels;
using Unity;
using Unity.Extension;

namespace PoeChatWheel.Prism
{
    internal sealed class PoeChatWheelRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<IPoeChatWheelViewModel, PoeChatWheelViewModel>();
        }
    }
}