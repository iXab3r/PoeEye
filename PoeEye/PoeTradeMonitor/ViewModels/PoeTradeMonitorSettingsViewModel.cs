using System.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.UI.ViewModels;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeTradeMonitorSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeTradeMonitorConfig>
    {
        private readonly IPoeMacroCommandsProvider commandsProvider;
        private readonly DelegateCommand<MacroMessageViewModel> removeMessageCommand;
        private readonly DelegateCommand addMessageCommand;

        private PoeTradeMonitorConfig loadedConfig;

        public PoeTradeMonitorSettingsViewModel(
            [NotNull] IPoeMacroCommandsProvider commandsProvider,
            [NotNull] IAudioNotificationSelectorViewModel audioNotificationSelector)
        {
            Guard.ArgumentNotNull(() => commandsProvider);
            Guard.ArgumentNotNull(() => audioNotificationSelector);

            this.commandsProvider = commandsProvider;
            AudioNotificationSelector = audioNotificationSelector;
            removeMessageCommand = new DelegateCommand<MacroMessageViewModel>(RemoveMessageCommandExecuted);
            addMessageCommand = new DelegateCommand(AddMessageCommandExecuted);
        }

        public IReactiveList<MacroMessageViewModel> PredefinedMessages { get; } = new ReactiveList<MacroMessageViewModel>();

        public ICommand RemoveMessageCommand => removeMessageCommand;

        public ICommand AddMessageCommand => addMessageCommand;

        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public string ModuleName { get; } = "Trade Monitor";

        public IReactiveList<MacroCommand> MacroCommands => commandsProvider.MacroCommands;
        private bool isEnabled;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { this.RaiseAndSetIfChanged(ref isEnabled, value); }
        }

        public void Load(PoeTradeMonitorConfig config)
        {
            Guard.ArgumentNotNull(() => config);

            loadedConfig = config;
            IsEnabled = config.IsEnabled;
            AudioNotificationSelector.SelectedValue = config.NotificationType;

            PredefinedMessages.Clear();
            config.PredefinedMessages.Select(x => new MacroMessageViewModel(x)).ForEach(PredefinedMessages.Add);
            if (PredefinedMessages.IsEmpty)
            {
                PoeTradeMonitorConfig.Default.PredefinedMessages.Select(x => new MacroMessageViewModel(x)).ForEach(PredefinedMessages.Add);
            }
        }

        public PoeTradeMonitorConfig Save()
        {
            loadedConfig.PredefinedMessages = PredefinedMessages
                .Where(IsValid)
                .Select(x => x.ToMessage())
                .ToList();
            loadedConfig.IsEnabled = IsEnabled;
            loadedConfig.NotificationType = AudioNotificationSelector.SelectedValue;
            return loadedConfig;
        }

        private bool IsValid(MacroMessageViewModel message)
        {
            return !string.IsNullOrWhiteSpace(message.Text) && !string.IsNullOrWhiteSpace(message.Label);
        }

        private void AddMessageCommandExecuted()
        {
            var newMessage = new MacroMessageViewModel();
            PredefinedMessages.Add(newMessage);
        }

        private void RemoveMessageCommandExecuted(MacroMessageViewModel macroMessage)
        {
            if (macroMessage == null)
            {
                return;
            }
            RemoveMessage(macroMessage);
        }

        private void RemoveMessage(MacroMessageViewModel macroMessage)
        {
            PredefinedMessages.Remove(macroMessage);
        }
    }
}