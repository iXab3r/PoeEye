using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Unity;
using PoeChatWheel.Prism;
using PoeChatWheel.ViewModels;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor.Prism;

namespace PoeChatWheel
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var container = new UnityContainer();

            new PoeChatWheelModule(container).Initialize();
            new PoeWhisperMonitorModule(container).Initialize();
            container.AddExtension(new CommonRegistrations());
            container.RegisterWindowTracker(WellKnownWindows.PathOfExile, () => "Path of Exile");

            var viewModel = container.Resolve<IPoeChatWheelViewModel>();
            var window =
                container.Resolve<ChatWheelWindow>(new DependencyOverride(typeof(IPoeChatWheelViewModel), viewModel));
            window.Show();
        }
    }
}