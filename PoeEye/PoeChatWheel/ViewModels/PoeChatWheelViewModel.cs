using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Gma.System.MouseKeyHook;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using RadialMenu.Controls;
using ReactiveUI;
using Stateless;
using Application = System.Windows.Application;
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

        private static readonly TimeSpan HistoryPeriod = TimeSpan.FromMinutes(2);

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> actionSelectedTransitionTrigger;

        private readonly IPoeChatService chatService;
        private readonly IClock clock;

        private readonly Collection<PoeMessage> messagesHistory = new Collection<PoeMessage>();
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> nameSelectedTransitionTrigger;

        private readonly StateMachine<State, Trigger> queryStateMachine = new StateMachine<State, Trigger>(State.Hidden);

        private readonly ConcurrentQueue<Color> userColors =
            new ConcurrentQueue<Color>(PleasantColors.Select(x => (Color) ColorConverter.ConvertFromString(x)));

        private RadialMenuCentralItem centralItem;

        private KeyGesture hotkey;
        private bool isOpen;

        public PoeChatWheelViewModel(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IPoeChatService chatService,
            [NotNull] [Dependency(WellKnownWindows.PathOfExile)] IWindowTracker poeWindowTracker,
            [NotNull] IClock clock)
        {
            this.chatService = chatService;
            this.clock = clock;
            var globalEvents = Hook.GlobalEvents();
            globalEvents.AddTo(Anchors);

            nameSelectedTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.NameSelected);
            actionSelectedTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.ActionSelected);

            queryStateMachine
                .Configure(State.Hidden)
                .Permit(Trigger.Show, State.NameSelection)
                .OnEntry(
                    () =>
                    {
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
                        IsOpen = false;
                    });

            Observable.FromEventPattern<WinFormsKeyEventHandler, WinFormsKeyEventArgs>(
                h => globalEvents.KeyDown += h,
                h => globalEvents.KeyDown -= h)
                .Where(x => queryStateMachine.State == State.Hidden)
                .Where(x => poeWindowTracker.IsActive)
                .Where(x => MatchesHotkey(x.EventArgs, hotkey))
                .Subscribe(
                    () =>
                    {
                        queryStateMachine.Fire(Trigger.Show);
                    })
                .AddTo(Anchors);

            Observable.FromEventPattern<WinFormsKeyEventHandler, WinFormsKeyEventArgs>(
                h => globalEvents.KeyUp += h,
                h => globalEvents.KeyUp -= h)
                .Where(x => queryStateMachine.State != State.Hidden)
                .Where(x => MatchesHotkey(x.EventArgs, hotkey))
                .Subscribe(
                    () =>
                    {
                        queryStateMachine.Fire(Trigger.Hide);
                    })
                .AddTo(Anchors);

            whisperService.Messages.Where(x => x.MessageType == PoeMessageType.WhisperFrom)
                .Subscribe(ProcessMessage)
                .AddTo(Anchors);
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

        public KeyGesture Hotkey
        {
            get { return hotkey; }
            set { this.RaiseAndSetIfChanged(ref hotkey, value); }
        }

        public IReactiveList<RadialMenuItem> Items { get; } = new ReactiveList<RadialMenuItem>
        {
            ChangeTrackingEnabled = true
        };

        private bool MatchesHotkey(WinFormsKeyEventArgs args, KeyGesture hotkey)
        {
            if (args == null || hotkey == null)
            {
                return false;
            }
            var winKey = (Keys) KeyInterop.VirtualKeyFromKey(hotkey.Key);
            var keyMatches = args.KeyCode == winKey;
            var wpfModifiers = ModifierKeys.None;
            if (args.Alt && winKey != Keys.Alt)
            {
                wpfModifiers |= ModifierKeys.Alt;
            }
            if (args.Control && winKey != Keys.LControlKey && winKey != Keys.RControlKey)
            {
                wpfModifiers |= ModifierKeys.Control;
            }
            if (args.Shift && winKey != Keys.Shift && winKey != Keys.ShiftKey && winKey != Keys.RShiftKey &&
                winKey != Keys.LShiftKey)
            {
                wpfModifiers |= ModifierKeys.Shift;
            }
            return keyMatches && wpfModifiers == hotkey.Modifiers;
        }

        private void RebuildCharactersList()
        {
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
            Items.Clear();
            Items.AddRange(menuItems);
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
            menuItem.Click += delegate
            {
                Log.Instance.Debug($"[PoeChatWheel.SelectCharacter] Name '{message.Name}'");
                queryStateMachine.Fire(nameSelectedTransitionTrigger, message.Name);
            };

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
            Items.Clear();
            Items.AddRange(menuItems);

            var central = new RadialMenuCentralItem();
            central.Tag = characterName;
            central.Content = CreateMenuItemContent(characterName, FontAwesome.Net.FontAwesome.user);
            central.ToolTip = PrepareMessageHistory(characterName);
            CentralItem = central;
        }

        private RadialMenuItem ToActionMenuItem(RadialMenuItem item)
        {
            if (!(item.Tag is string))
            {
                return item;
            }
            item.Click += delegate
            {
                Log.Instance.Debug($"[PoeChatWheel.SelectAction] Action '{item.Tag}'");
                queryStateMachine.Fire(actionSelectedTransitionTrigger, (string) item.Tag);
            };
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
            var panel = new WrapPanel {Orientation = Orientation.Vertical};
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