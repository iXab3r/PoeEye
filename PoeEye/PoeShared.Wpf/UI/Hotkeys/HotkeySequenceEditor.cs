using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop.Utilities;
using Microsoft.VisualBasic;
using PoeShared.Scaffolding;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;

namespace PoeShared.UI.Hotkeys
{
    public partial class HotkeySequenceEditor
    {
        private ListBox listBox;

        public HotkeySequenceEditor()
        {
            Actions = new HotkeySequenceActions(this);
            DropTarget = new HotkeyDropHandler(this);

            this.Observe(ItemsSourceProperty)
                .Select(x => ItemsSource)
                .Select(x => x != null
                    ? new ListCollectionView(x)
                    {
                        NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd
                    }
                    : new ListCollectionView(new Collection()))
                .Subscribe(x => CollectionView = x)
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
            if (e.Key == Key.Delete)
            {
                var selectedItems = listBox.SelectedItems.OfType<HotkeySequenceItem>().ToArray();
                var itemsSource = ItemsSource;
                if (selectedItems.Any())
                {
                    selectedItems.ForEach(itemsSource.Remove);
                    e.Handled = true;
                }
            }
        }
    }
}