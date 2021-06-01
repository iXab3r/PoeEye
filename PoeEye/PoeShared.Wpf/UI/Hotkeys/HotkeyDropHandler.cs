using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class HotkeyDropHandler : DefaultDropHandler
    {
        private readonly HotkeySequenceEditor owner;

        public HotkeyDropHandler(HotkeySequenceEditor owner)
        {
            this.owner = owner;
        }
        
        public override void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.DragInfo.VisualSource is Button button && button.CommandParameter != null)
            {
                var dropIndex = GetIndex(dropInfo);
                var itemsSource = owner.ItemsSource as ObservableCollection<HotkeySequenceItem> ?? throw new InvalidOperationException($"Owner's item source is not set or invalid: {owner.ItemsSource}");
                switch (button.CommandParameter)
                {
                    case Key key:
                        new HotkeySequenceHotkey()
                        {
                            Hotkey = new HotkeyGesture(key),
                            IsDown = true
                        }.InsertTo(itemsSource, dropIndex++);
                        new HotkeySequenceDelay()
                        {
                            IsKeypress = true,
                            Delay = TimeSpan.FromMilliseconds(50)
                        }.InsertTo(itemsSource, dropIndex++);
                        new HotkeySequenceHotkey()
                        {
                            Hotkey = new HotkeyGesture(key),
                            IsDown = false
                        }.InsertTo(itemsSource,  dropIndex);
                        break;
                    case HotkeySequenceItem item:
                    {
                        var clone = item.Clone() as HotkeySequenceItem;
                        clone.InsertTo(itemsSource, dropIndex);
                        break;
                    }
                }
                return;
            }
            base.Drop(dropInfo);
        }

        public override void DragOver(IDropInfo dropInfo)
        {
            base.DragOver(dropInfo);
        }

        private int GetIndex(IDropInfo dropInfo)
        {
            var insertIndex = dropInfo.UnfilteredInsertIndex;

            if (dropInfo.VisualTarget is ItemsControl {Items: IEditableCollectionView editableItems} itemsControl)
            {
                var newItemPlaceholderPosition = editableItems.NewItemPlaceholderPosition;
                switch (newItemPlaceholderPosition)
                {
                    case NewItemPlaceholderPosition.AtBeginning when insertIndex == 0:
                        ++insertIndex;
                        break;
                    case NewItemPlaceholderPosition.AtEnd when insertIndex == itemsControl.Items.Count:
                        --insertIndex;
                        break;
                }
            }

            return insertIndex;
        }
    }
}