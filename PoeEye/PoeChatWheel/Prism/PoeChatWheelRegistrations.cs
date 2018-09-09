using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeChatWheel.ViewModels;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
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