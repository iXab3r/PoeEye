using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Gma.System.MouseKeyHook;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeChatWheel.Modularity;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using RadialMenu.Controls;
using ReactiveUI;
using Stateless;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using Orientation = System.Windows.Controls.Orientation;
using WinFormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WinFormsKeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeChatWheel.ViewModels
{
    internal sealed class PoeChatWheelViewModel : DisposableReactiveObject, IPoeChatWheelViewModel
    {
        private static readonly string[] PleasantColors =
        {
            "#001f3f",
            "#0074D9",
            "#7FDBFF",
            "#39CCCC",
            "#3D9970",
            "#2ECC40",
            "#01FF70",
            "#FFDC00",
            "#FF851B",
            "#FF4136"
        };

        private readonly IPoeChatService chatService;
        private readonly IClock clock;

        private readonly Collection<PoeMessage> messagesHistory = new Collection<PoeMessage>();

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> actionSelectedTransitionTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> nameSelectedTransitionTrigger;
        private readonly StateMachine<State, Trigger> queryStateMachine = new StateMachine<State, Trigger>(State.Hidden);

        private readonly ConcurrentQueue<Color> userColors =
            new ConcurrentQueue<Color>(PleasantColors.Select(x => (Color) ColorConverter.ConvertFromString(x)));

        private RadialMenuCentralItem centralItem;

        private TimeSpan historyPeriod = TimeSpan.FromMinutes(2);

        private KeyGesture hotkey;
        private bool isOpen;

        public PoeChatWheelViewModel(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IPoeChatService chatService,
            [NotNull] IConfigProvider<PoeChatWheelConfig> configProvider,
            [NotNull] [Dependency(WellKnownWindows.PathOfExile)] IWindowTracker poeWindowTracker,
            [NotNull] IClock clock)
        {
            Log.Instance.Debug($"[PoeChatWheel..ctor] Initializing chat wheel...");
            this.chatService = chatService;
            this.clock = clock;

            var globalEvents = Hook.GlobalEvents();
            globalEvents.AddTo(Anchors);

            nameSelectedTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.NameSelected);
            actionSelectedTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.ActionSelected);

            queryStateMachine.OnTransitioned(
                x =>
                    Log.Instance.Debug(
                        $"[PoeChatWheel.Transition] Trigger: {x.Trigger} {x.Source} -> {x.Destination} (isReentry: {x.IsReentry})"));

            queryStateMachine
                .Configure(State.Hidden)
                .Permit(Trigger.Show, State.NameSelection)
                .OnEntry(
                    () =>
                    {
                        Log.Instance.Debug($"[PoeChatWheel.OnHidden] IsOpen: {IsOpen} => False");
                        IsOpen = false;
                    });

            queryStateMachine
                .Configure(State.NameSelection)
                .Permit(Trigger.NameSelected, State.ActionSelection)
                .Permit(Trigger.ActionSelected, State.Done)
                .Permit(Trigger.Hide, State.Hidden)
                .OnEntry(RebuildCharactersList)
                .OnEntry(
                    () =>
                    {
                        Log.Instance.Debug($"[PoeChatWheel.OnNameSelection] IsOpen: {IsOpen} => True");
                        IsOpen = true;
                    });

            queryStateMachine
                .Configure(State.ActionSelection)
                .Permit(Trigger.ActionSelected, State.Done)
                .Permit(Trigger.Hide, State.Hidden)
                .OnEntryFrom(nameSelectedTransitionTrigger, RebuildMenuForCharacter);

            queryStateMachine
                .Configure(State.Done)
                .Permit(Trigger.Hide, State.Hidden)
                .Ignore(Trigger.ActionSelected)
                .OnEntryFrom(actionSelectedTransitionTrigger, SendMessage)
                .OnEntry(
                    () =>
                    {
                        Log.Instance.Debug($"[PoeChatWheel.OnDone] IsOpen: {IsOpen} => False");
                        IsOpen = false;
                    });

            Observable.FromEventPattern<WinFormsKeyEventHandler, WinFormsKeyEventArgs>(
                h => globalEvents.KeyDown += h,
                h => globalEvents.KeyDown -= h)
                .Where(x => queryStateMachine.State == State.Hidden)
                .Where(x => poeWindowTracker.IsActive)
                .Where(x => MatchesHotkey(x.EventArgs, hotkey))
                .Subscribe(
                    () => { queryStateMachine.Fire(Trigger.Show); })
                .AddTo(Anchors);

            Observable.FromEventPattern<WinFormsKeyEventHandler, WinFormsKeyEventArgs>(
                h => globalEvents.KeyUp += h,
                h => globalEvents.KeyUp -= h)
                .Where(x => queryStateMachine.State != State.Hidden)
                .Where(x => MatchesHotkey(x.EventArgs, hotkey))
                .Subscribe(
                    () => { queryStateMachine.Fire(Trigger.Hide); })
                .AddTo(Anchors);

            whisperService.Messages.Where(x => x.MessageType == PoeMessageType.WhisperFrom)
                .Subscribe(ProcessMessage)
                .AddTo(Anchors);

            configProvider.WhenAnyValue(x => x.ActualConfig)
                          .Select(x => x.ChatWheelHotkey)
                          .Select(hotkey => new KeyGestureConverter().ConvertFromInvariantString(hotkey) as KeyGesture)
                          .Subscribe(x => this.hotkey = x)
                          .AddTo(Anchors);
        }

        public TimeSpan HistoryPeriod
        {
            get { return historyPeriod; }
            set { this.RaiseAndSetIfChanged(ref historyPeriod, value); }
        }

        public RadialMenuCentralItem CentralItem
        {
            get { return centralItem; }
            set { this.RaiseAndSetIfChanged(ref centralItem, value); }
        }

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public IReactiveList<RadialMenuItem> Items { get; } = new ReactiveList<RadialMenuItem>();

        private bool MatchesHotkey(WinFormsKeyEventArgs args, KeyGesture candidate)
        {
            if (args == null || candidate == null)
            {
                return false;
            }
            return candidate.MatchesHotkey(args);
        }

        private void RebuildCharactersList()
        {
            Log.Instance.Debug($"[PoeChatWheel.RebuildCharactersList] Rebuilding characters list");
            CleanupHistory();

            var menuItems = messagesHistory.Select(ToMenuItem).ToList();
            menuItems.Add(
                ToActionMenuItem(
                    new RadialMenuItem
                    {
                        Content = CreateMenuItemContent("Go home", FontAwesome.Net.FontAwesome.home),
                        Tag = $"/hideout"
                    }));
            menuItems.Add(
                ToActionMenuItem(
                    new RadialMenuItem
                    {
                        Content = CreateMenuItemContent("Reset XP", FontAwesome.Net.FontAwesome.gears),
                        Tag = $"/reset_xp"
                    }));
            using (Items.SuppressChangeNotifications())
            {
                Items.Clear();
                Items.AddRange(menuItems);
            }
            CentralItem = null;
        }

        private void CleanupHistory()
        {
            var messagesToRemove = messagesHistory.Where(x => clock.Now - x.Timestamp > HistoryPeriod).ToArray();
            foreach (var poeMessage in messagesToRemove)
            {
                messagesHistory.Remove(poeMessage);
            }
        }

        private RadialMenuItem ToMenuItem(PoeMessage message)
        {
            var timeElapsed = clock.Now - message.Timestamp;

            var menuItem = new RadialMenuItem();
            var characterName = message.Name?.Substring(0, Math.Min(8, message.Name?.Length ?? 0));
            menuItem.Tag = message;
            menuItem.Content = CreateMenuItemContent(
                $"{characterName}\n\xF017 {timeElapsed.TotalSeconds:F0}s ago", FontAwesome.Net.FontAwesome.user);

            Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => menuItem.MouseEnter += h,
                h => menuItem.MouseEnter -= h)
                .Where(x => IsOpen)
                .Subscribe(
                    () =>
                    {
                        Log.Instance.Debug($"[PoeChatWheel.SelectCharacter] Name '{message.Name}'");
                        queryStateMachine.Fire(nameSelectedTransitionTrigger, message.Name);
                    });

            menuItem.ToolTip = PrepareMessageHistory(message.Name);
            return menuItem;
        }

        private UIElement PrepareMessageHistory(string characterName)
        {
            var result = new ItemsControl();
            foreach (var source in messagesHistory.Where(x => x.Name == characterName).ToArray())
            {
                result.Items.Add(source);
            }
            return result;
        }

        private void ProcessMessage(PoeMessage message)
        {
            CleanupHistory();
            if (messagesHistory.Select(x => x.Name).Contains(message.Name))
            {
                return;
            }

            messagesHistory.Add(message);
        }

        private Color PickNextUserColor()
        {
            Color color;
            if (userColors.TryDequeue(out color))
            {
                userColors.Enqueue(color);
                return color;
            }
            return Colors.White;
        }

        private void RebuildMenuForCharacter(string characterName)
        {
            Log.Instance.Debug($"[PoeChatWheel.RebuildMenuForCharacter] Rebuilding items for character {characterName}");

            var menuItems = new[]
            {
                new RadialMenuItem
                {
                    Content = CreateMenuItemContent("Invite", FontAwesome.Net.FontAwesome.group),
                    Tag = $"/invite {characterName}"
                },
                new RadialMenuItem
                {
                    Content = CreateMenuItemContent("Thanks", FontAwesome.Net.FontAwesome.thumbs_up),
                    Tag = $"@{characterName} thanks"
                },
                new RadialMenuItem
                {
                    Content = CreateMenuItemContent("No thanks", FontAwesome.Net.FontAwesome.paper_plane_o),
                    Tag = $"@{characterName} no, thanks"
                },
                new RadialMenuItem
                {
                    Content = CreateMenuItemContent("Wait", FontAwesome.Net.FontAwesome.refresh),
                    Tag = $"@{characterName} please 1-2mins, busy atm"
                }
            };

            foreach (var radialMenuItem in menuItems)
            {
                ToActionMenuItem(radialMenuItem);
            }
            using (Items.SuppressChangeNotifications())
            {
                Items.Clear();
                Items.AddRange(menuItems);
            }

            var central = new RadialMenuCentralItem
            {
                Tag = characterName,
                Content = CreateMenuItemContent(characterName, FontAwesome.Net.FontAwesome.user),
                ToolTip = PrepareMessageHistory(characterName)
            };
            CentralItem = central;
        }

        private RadialMenuItem ToActionMenuItem(RadialMenuItem item)
        {
            if (!(item.Tag is string))
            {
                return item;
            }

            Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => item.PreviewMouseDown += h,
                h => item.PreviewMouseDown -= h)
                .Where(x => IsOpen)
                .Where(x => x.EventArgs.ClickCount == 1 && x.EventArgs.LeftButton == MouseButtonState.Pressed)
                .Subscribe(
                    x =>
                    {
                        Log.Instance.Debug($"[PoeChatWheel.SelectAction] Action '{item.Tag}'");
                        x.EventArgs.Handled = true;
                        queryStateMachine.Fire(actionSelectedTransitionTrigger, (string)item.Tag);
                    });
            return item;
        }

        private UIElement CreateMenuItemContent(string itemText)
        {
            return new TextBlock
            {
                Text = itemText,
                TextAlignment = TextAlignment.Center,
                FontFamily = Application.Current.FindResource("FontAwesome") as FontFamily,
                Width = 80
            };
        }

        private UIElement CreateMenuItemContent(string itemText, string itemIcon)
        {
            var panel = new WrapPanel { Orientation = Orientation.Vertical };
            var text = CreateMenuItemContent(itemText);
            var icon = new TextBlock
            {
                Text = itemIcon,
                FontSize = 30,
                Foreground = new SolidColorBrush(PickNextUserColor()),
                FontFamily = Application.Current.FindResource("FontAwesome") as FontFamily
            };

            panel.Children.Add(icon);
            panel.Children.Add(text);
            return panel;
        }

        private void SendMessage(string message)
        {
            Log.Instance.Debug($"[PoeChatWheel.SendMessage] Sending message '{message}'");

            chatService.SendMessage(message);
        }

        private enum State
        {
            Hidden,
            NameSelection,
            ActionSelection,
            Done
        }

        private enum Trigger
        {
            Show,
            NameSelected,
            ActionSelected,
            Hide
        }
    }
}