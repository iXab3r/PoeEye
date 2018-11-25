using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services;
using PoeShared.Audio.Services;
using PoeShared.Audio.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.ViewModels;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeTradeMonitorSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeTradeMonitorConfig>
    {
        private readonly DelegateCommand addMessageCommand;
        private readonly IPoeMacroCommandsProvider commandsProvider;
        private readonly DelegateCommand<MacroMessageViewModel> removeMessageCommand;

        private readonly PoeTradeMonitorConfig temporaryConfig = new PoeTradeMonitorConfig();
        private bool isEnabled;

        public PoeTradeMonitorSettingsViewModel(
            [NotNull] IPoeMacroCommandsProvider commandsProvider,
            [NotNull] IAudioNotificationSelectorViewModel audioNotificationSelector)
        {
            Guard.ArgumentNotNull(commandsProvider, nameof(commandsProvider));
            Guard.ArgumentNotNull(audioNotificationSelector, nameof(audioNotificationSelector));

            this.commandsProvider = commandsProvider;
            AudioNotificationSelector = audioNotificationSelector;
            removeMessageCommand = new DelegateCommand<MacroMessageViewModel>(RemoveMessageCommandExecuted);
            addMessageCommand = new DelegateCommand(AddMessageCommandExecuted);
        }

        public IReactiveList<MacroMessageViewModel> PredefinedMessages { get; } = new ReactiveList<MacroMessageViewModel>();

        public ICommand RemoveMessageCommand => removeMessageCommand;

        public ICommand AddMessageCommand => addMessageCommand;

        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public IReactiveList<MacroCommand> MacroCommands => commandsProvider.MacroCommands;

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public string ModuleName { get; } = "Trade Monitor";

        public async Task Load(PoeTradeMonitorConfig config)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            config.CopyPropertiesTo(temporaryConfig);

            IsEnabled = config.IsEnabled;
            AudioNotificationSelector.SelectedValue = config.NotificationType.ToString();

            PredefinedMessages.Clear();
            config.PredefinedMessages.Select(x => new MacroMessageViewModel(x)).ForEach(PredefinedMessages.Add);
            if (PredefinedMessages.IsEmpty)
            {
                PoeTradeMonitorConfig.Default.PredefinedMessages.Select(x => new MacroMessageViewModel(x)).ForEach(PredefinedMessages.Add);
            }
        }

        public PoeTradeMonitorConfig Save()
        {
            temporaryConfig.PredefinedMessages = PredefinedMessages
                                                 .Where(IsValid)
                                                 .Select(x => x.ToMessage())
                                                 .ToList();
            temporaryConfig.IsEnabled = IsEnabled;
            temporaryConfig.NotificationType = AudioNotificationSelector.SelectedValue.ParseEnumSafe<AudioNotificationType>();

            var result = new PoeTradeMonitorConfig();
            temporaryConfig.CopyPropertiesTo(result);

            return result;
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