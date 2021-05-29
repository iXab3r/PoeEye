using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public partial class HotkeySequenceEditor
    {
        private ListBox listBox;

        public HotkeySequenceEditor()
        {
            Actions = new HotkeySequenceActions(this);
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
            var itemsSource = (IList)ItemsSource;
            if (selectedItems.Any())
            {
                if (e.Key == Key.Delete)
                {
                    selectedItems.ForEach(itemsSource.Remove);
                    e.Handled = true;
                }
            }
        }
    }
}