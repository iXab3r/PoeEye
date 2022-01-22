using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualBasic;
using PoeShared.Scaffolding;
using ReactiveUI;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;

namespace PoeShared.UI;

internal partial class HotkeySequenceEditor
{
    private ListBox listBox;

    public HotkeySequenceEditor()
    {
        DropTarget = new HotkeyDropHandler(this);
        DragSource = new HotkeyDragHandler();

        this.Observe(ViewModelProperty)
            .Select(x => ViewModel)
            .Select(x => x != null ? x.WhenAnyValue(y => y.Items) : Observable.Empty<ObservableCollection<HotkeySequenceItem>>())
            .Switch()
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
            var itemsSource = ViewModel?.Items;
            if (itemsSource != null && selectedItems.Any())
            {
                selectedItems.ForEach(x => itemsSource.Remove(x));
                e.Handled = true;
            }
        }
    }
}