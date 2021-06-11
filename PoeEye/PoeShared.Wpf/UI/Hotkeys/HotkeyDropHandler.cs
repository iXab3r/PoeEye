using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using PoeShared.Scaffolding;

namespace PoeShared.UI
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
                var itemsSource = owner.ViewModel ?? throw new InvalidOperationException($"Owner's item source is not set or invalid: {owner.ViewModel}");
                switch (button.CommandParameter)
                {
                    case Key key:
                        new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(key),
                            IsDown = true
                        }.InsertTo(itemsSource.Items, dropIndex++);
                        new HotkeySequenceDelay
                        {
                            IsKeypress = true,
                            Delay = itemsSource.DefaultKeyPressDuration
                        }.InsertTo(itemsSource.Items, dropIndex++);
                        new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(key),
                            IsDown = false
                        }.InsertTo(itemsSource.Items,  dropIndex);
                        break;
                    case MouseButton mouseButton:
                        new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(mouseButton),
                            IsDown = true
                        }.InsertTo(itemsSource.Items, dropIndex++);
                        new HotkeySequenceDelay
                        {
                            IsKeypress = true,
                            Delay = itemsSource.DefaultKeyPressDuration
                        }.InsertTo(itemsSource.Items, dropIndex++);
                        new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(mouseButton),
                            IsDown = false
                        }.InsertTo(itemsSource.Items,  dropIndex);
                        break;
                    case HotkeySequenceItem item:
                    {
                        var clone = item.Clone() as HotkeySequenceItem;
                        clone.InsertTo(itemsSource.Items, dropIndex);
                        break;
                    }
                }
                return;
            }
            base.Drop(dropInfo);
        }

        private static int GetIndex(IDropInfo dropInfo)
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