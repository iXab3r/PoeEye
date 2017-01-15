using System.Windows;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeChatWheel.Modularity;
using PoeChatWheel.ViewModels;
using PoeShared.Modularity;

namespace PoeChatWheel.Prism
{
    public sealed class PoeChatWheelModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeChatWheelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeChatWheelRegistrations());

            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeChatWheelConfig, PoeChatWheelSettingsViewModel>();

            var chatWheel = container.Resolve<IPoeChatWheelViewModel>();

            var window = new ChatWheelWindow(chatWheel);
            window.Show();

            var mainWindow = Application.Current.MainWindow;
            mainWindow.Closed += delegate { window.Close(); };
        }
    }
}