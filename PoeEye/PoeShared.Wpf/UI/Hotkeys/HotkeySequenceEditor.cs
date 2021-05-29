using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using log4net;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public partial class HotkeySequenceEditor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeySequenceEditor));

        private readonly SourceList<HotkeySequenceItem> itemsSource = new();

        private ListBox listBox;

        public HotkeySequenceEditor()
        {
            Actions = new HotkeySequenceActions(this, itemsSource);
            var items = new ObservableCollectionExtended<HotkeySequenceItem>();

            itemsSource.Add(new HotkeySequenceHotkey() { Hotkey = new HotkeyGesture(Key.Cancel)});
            itemsSource.Add(new HotkeySequenceHotkey() { Hotkey = new HotkeyGesture(Key.A)});
            itemsSource
                .Connect()
                .Bind(items)
                .Subscribe()
                .AddTo(Anchors);
            Items = items;
            FooterItems = new ReadOnlyObservableCollection<HotkeySequenceItem>(new ObservableCollection<HotkeySequenceItem>(new[] {Actions}));
            itemsSource.Add(new HotkeySequenceHotkey() {  Hotkey = new HotkeyGesture(Key.LeftShift) });

            this.Observe(KeyboardEventsSourceProperty)
                .Select(x => KeyboardEventsSource)
                .SubscribeSafe(x => Actions.KeyboardEventSource = x, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public override void OnApplyTemplate()
        {
            if (listBox != null)
            {
                listBox.KeyDown -= ListBoxOnKeyDown;
            }

            base.OnApplyTemplate();

            listBox = (ListBox) Template.FindName(PART_ListBox, this);
            if (listBox == null)
            {
                return;
            }

            listBox.KeyDown += ListBoxOnKeyDown;
        }

        private void ListBoxOnKeyDown(object sender, KeyEventArgs e)
        {
            var selectedItems = listBox.SelectedItems.OfType<HotkeySequenceItem>().ToArray();
            if (selectedItems.Any())
            {
                if (e.Key == Key.Delete)
                {
                    itemsSource.RemoveMany(selectedItems);
                    e.Handled = true;
                }
                else if (e.Key == Key.Left)
                {
                    foreach (var hotkeySequenceItem in selectedItems)
                    {
                        itemsSource.Edit(x =>
                        {
                            var index = x.IndexOf(hotkeySequenceItem);
                            var newIndex = Math.Max(0, index - 1);
                            if (index != newIndex)
                            {
                                itemsSource.Move(index, newIndex);
                            }
                        });
                    }
                }
                else if (e.Key == Key.Right)
                {
                    foreach (var hotkeySequenceItem in selectedItems)
                    {
                        itemsSource.Edit(x =>
                        {
                            var index = x.IndexOf(hotkeySequenceItem);
                            var newIndex = Math.Min(x.Count, index + 1);
                            if (index != newIndex)
                            {
                                itemsSource.Move(index, newIndex);
                            }
                        });
                    }
                }
            }
        }
    }
}