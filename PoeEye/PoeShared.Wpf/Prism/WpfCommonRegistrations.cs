using System.Reactive.Concurrency;
using PoeShared.Audio.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.UI.Hotkeys;
using PoeShared.Wpf.UI.Settings;
using ReactiveUI;
using Unity;
using Unity.Extension;

namespace PoeShared.Prism
{
    public sealed class WpfCommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterFactory<IScheduler>(WellKnownSchedulers.UI, x => RxApp.MainThreadScheduler)
                .RegisterFactory<IScheduler>(WellKnownSchedulers.Background, x => RxApp.TaskpoolScheduler);

            Container
                .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
                .RegisterSingleton<ISchedulerProvider, SchedulerProvider>()
                .RegisterSingleton<IHotkeyConverter, HotkeyConverter>();

            Container
                .RegisterType<IHotkeyTracker, HotkeyTracker>()
                .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
        }
    }
}