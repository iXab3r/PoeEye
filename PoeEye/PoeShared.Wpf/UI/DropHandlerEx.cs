using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using DragDrop = GongSolutions.Wpf.DragDrop.DragDrop;

namespace PoeShared.UI;

internal sealed class DropHandlerEx : DefaultDropHandler
{
    public static readonly DropHandlerEx Instance = new DropHandlerEx();

    public override void DragOver(IDropInfo dropInfo)
    {
        if (!CanAcceptData(dropInfo))
        {
            return;
        }

        var dragSourceIgnore = dropInfo.VisualTargetItem == null ? default(bool?) : DragDrop.GetDragSourceIgnore(dropInfo.VisualTargetItem);
        if (dragSourceIgnore == true)
        {
            return;
        }


        dropInfo.Effects = ShouldCopyData(dropInfo) ? DragDropEffects.Copy : DragDropEffects.Move;
        var isTreeViewItem = dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) && dropInfo.VisualTargetItem is TreeViewItem;
        dropInfo.DropTargetAdorner = isTreeViewItem ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
    }

    public override void Drop(IDropInfo dropInfo)
    {
        var targetList = FindList(dropInfo.TargetCollection);
        var sourceList = FindList(dropInfo.DragInfo.SourceCollection);
        if (targetList == null)
        {
            return;
        }

        if (sourceList == null && dropInfo.Effects.HasFlag(DragDropEffects.Move))
        {
            return;
        }

        if (dropInfo.InsertIndex < 0 || dropInfo.InsertIndex -1 > targetList.Count)
        {
            return;
        }

            
        InternalDrop(dropInfo);
    }
        
    private void InternalDrop(IDropInfo dropInfo)
    {
        if (dropInfo?.DragInfo == null)
        {
            return;
        }

        var insertIndex = dropInfo.InsertIndex;
        var itemsControl = dropInfo.VisualTarget as ItemsControl;
        if (itemsControl?.Items is IEditableCollectionView editableItems)
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

        var destinationList = FindList(dropInfo.TargetCollection);
        var data = ExtractData(dropInfo.Data)
            .OfType<object>()
            .SelectMany(x => (x is IEnumerable enumerable) ? enumerable.OfType<object>() : new[] { x })
            .ToList();

        if (!ShouldCopyData(dropInfo))
        {
            var sourceList = FindList(dropInfo.DragInfo.SourceCollection);
            if (sourceList != null)
            {
                foreach (var index in data.Select(o => sourceList.IndexOf(o)).Where(index => index != -1))
                {
                    sourceList.RemoveAt(index);
                    // so, is the source list the destination list too ?
                    if (destinationList != null && Equals(sourceList, destinationList) && index < insertIndex)
                    {
                        --insertIndex;
                    }
                }
            }
        }

        if (destinationList == null)
        {
            return;
        }

        var objects2Insert = new List<object>();

        // check for cloning
        var cloneData = dropInfo.Effects.HasFlag(DragDropEffects.Copy) || dropInfo.Effects.HasFlag(DragDropEffects.Link);
        foreach (var o in data)
        {
            var obj2Insert = o;
            if (cloneData)
            {
                if (o is ICloneable cloneable)
                {
                    obj2Insert = cloneable.Clone();
                }
            }

            objects2Insert.Add(obj2Insert);
            destinationList.Insert(insertIndex++, obj2Insert);
        }

        var selectDroppedItems = itemsControl is TabControl || itemsControl != null && DragDrop.GetSelectDroppedItems(itemsControl);
        if (selectDroppedItems)
        {
            SelectDroppedItems(dropInfo, objects2Insert);
        }
    }

    private IList FindList(IEnumerable enumerable)
    {
        if (enumerable is CompositeCollection compositeCollection)
        {
            foreach (var items in compositeCollection.OfType<CollectionContainer>().Select(x => x.Collection).Select(x => x.TryGetList()).Where(x => x != null))
            {
                return items;
            }
            return null;
        }

        return enumerable.TryGetList();
    }
}