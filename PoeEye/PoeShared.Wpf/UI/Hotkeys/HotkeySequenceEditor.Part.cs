using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;

namespace PoeShared.UI;

[TemplatePart(Name = PART_ListBox, Type = typeof(ListBox))]
internal partial class HotkeySequenceEditor : ReactiveControl
{
    public const string PART_ListBox = "PART_ListBox";

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        "ViewModel", typeof(IHotkeySequenceEditorViewModel), typeof(HotkeySequenceEditor), new PropertyMetadata(default(IHotkeySequenceEditorViewModel)));

    internal static readonly DependencyProperty CollectionViewProperty = DependencyProperty.Register(
        "CollectionView", typeof(ListCollectionView), typeof(HotkeySequenceEditor), new PropertyMetadata(default(ListCollectionView)));

    internal static readonly DependencyProperty DropTargetProperty = DependencyProperty.Register(
        "DropTarget", typeof(IDropTarget), typeof(HotkeySequenceEditor), new PropertyMetadata(default(IDropTarget)));
        
    internal static readonly DependencyProperty DragSourceProperty = DependencyProperty.Register(
        "DragSource", typeof(IDragSource), typeof(HotkeySequenceEditor), new PropertyMetadata(default(IDragSource)));

    public IHotkeySequenceEditorViewModel ViewModel
    {
        get { return (IHotkeySequenceEditorViewModel) GetValue(ViewModelProperty); }
        set { SetValue(ViewModelProperty, value); }
    }
        
    internal ListCollectionView CollectionView
    {
        get { return (ListCollectionView) GetValue(CollectionViewProperty); }
        set { SetValue(CollectionViewProperty, value); }
    }

    internal IDropTarget DropTarget
    {
        get { return (IDropTarget) GetValue(DropTargetProperty); }
        private set { SetValue(DropTargetProperty, value); }
    }

    internal IDragSource DragSource
    {
        get { return (IDragSource) GetValue(DragSourceProperty); }
        private set { SetValue(DragSourceProperty, value); }
    }
        
    static HotkeySequenceEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HotkeySequenceEditor), new FrameworkPropertyMetadata(typeof(HotkeySequenceEditor)));
    }
}