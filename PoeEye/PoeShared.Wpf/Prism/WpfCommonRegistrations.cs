using PoeShared.Audio.ViewModels;
using PoeShared.UI.Hotkeys;
using PoeShared.Wpf.UI.Settings;
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
                .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
        }
    }
}