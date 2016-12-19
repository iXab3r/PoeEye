using Microsoft.Practices.Unity;
using PoeChatWheel.ViewModels;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;

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