using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    [TemplatePart(Name = PART_ListBox, Type = typeof(ListBox))]
    public partial class HotkeySequenceEditor : ReactiveControl
    {
        public const string PART_ListBox = "PART_ListBox";

        public static readonly DependencyProperty KeyboardEventsSourceProperty = DependencyProperty.Register(
            "KeyboardEventsSource", typeof(IKeyboardEventsSource), typeof(HotkeySequenceEditor), new PropertyMetadata(default(IKeyboardEventsSource)));

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IEnumerable), typeof(HotkeySequenceEditor), new PropertyMetadata(default(IEnumerable)));

        public static readonly DependencyProperty HideKeypressDelaysProperty = DependencyProperty.Register(
            "HideKeypressDelays", typeof(bool), typeof(HotkeySequenceEditor), new PropertyMetadata(default(bool)));

        internal static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items", typeof(ObservableCollection<HotkeySequenceItem>), typeof(HotkeySequenceEditor), new PropertyMetadata(default(ObservableCollection<HotkeySequenceItem>)));

        internal static readonly DependencyProperty FooterItemsProperty = DependencyProperty.Register(
            "FooterItems", typeof(ReadOnlyObservableCollection<HotkeySequenceItem>), typeof(HotkeySequenceEditor), new PropertyMetadata(default(ReadOnlyObservableCollection<HotkeySequenceItem>)));

        public bool HideKeypressDelays
        {
            get { return (bool) GetValue(HideKeypressDelaysProperty); }
            set { SetValue(HideKeypressDelaysProperty, value); }
        }
        
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        internal ObservableCollection<HotkeySequenceItem> Items
        {
            get { return (ObservableCollection<HotkeySequenceItem>) GetValue(ItemsProperty); }
            private set { SetValue(ItemsProperty, value); }
        }

        internal ReadOnlyObservableCollection<HotkeySequenceItem> FooterItems
        {
            get { return (ReadOnlyObservableCollection<HotkeySequenceItem>) GetValue(FooterItemsProperty); }
            private set { SetValue(FooterItemsProperty, value); }
        }
        
        public IKeyboardEventsSource KeyboardEventsSource
        {
            get { return (IKeyboardEventsSource) GetValue(KeyboardEventsSourceProperty); }
            set { SetValue(KeyboardEventsSourceProperty, value); }
        }

        internal static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
            "Actions", typeof(HotkeySequenceActions), typeof(HotkeySequenceEditor), new PropertyMetadata(default(HotkeySequenceActions)));

        internal HotkeySequenceActions Actions
        {
            get { return (HotkeySequenceActions) GetValue(ActionsProperty); }
            private set { SetValue(ActionsProperty, value); }
        }
        
        static HotkeySequenceEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HotkeySequenceEditor), new FrameworkPropertyMetadata(typeof(HotkeySequenceEditor)));
        }
    }
}