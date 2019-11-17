using PoeShared.Audio.ViewModels;
using PoeShared.UI.Hotkeys;
using Unity;
using Unity.Extension;

namespace PoeShared.Prism
{
    public sealed class WpfCommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IHotkeyConverter, HotkeyConverter>();

            Container
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
        }
    }
}