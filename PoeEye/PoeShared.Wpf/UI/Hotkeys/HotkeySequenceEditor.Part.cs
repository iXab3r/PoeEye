using System;
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
            "ItemsSource", typeof(IEnumerable), typeof(HotkeySequenceEditor), new PropertyMetadata(new ObservableCollection<HotkeySequenceItem>()));

        public static readonly DependencyProperty HideKeypressDelaysProperty = DependencyProperty.Register(
            "HideKeypressDelays", typeof(bool), typeof(HotkeySequenceEditor), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty MaxRecordingDurationProperty = DependencyProperty.Register(
            "MaxRecordingDuration", typeof(TimeSpan), typeof(HotkeySequenceEditor), new PropertyMetadata(TimeSpan.FromSeconds(10)));
        
        public static readonly DependencyProperty MousePositionRecordingResolutionProperty = DependencyProperty.Register(
            "MousePositionRecordingResolution", typeof(TimeSpan), typeof(HotkeySequenceEditor), new PropertyMetadata(TimeSpan.FromMilliseconds(250)));

        public static readonly DependencyProperty EnableMousePositionRecordingProperty = DependencyProperty.Register(
            "EnableMousePositionRecording", typeof(bool), typeof(HotkeySequenceEditor), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty MaxItemsCountProperty = DependencyProperty.Register(
            "MaxItemsCount", typeof(int), typeof(HotkeySequenceEditor), new PropertyMetadata(250));
        
        public static readonly DependencyProperty EnableKeyboardRecordingProperty = DependencyProperty.Register(
            "EnableKeyboardRecording", typeof(bool), typeof(HotkeySequenceEditor), new PropertyMetadata(true));
        
        public static readonly DependencyProperty EnableMouseClicksRecordingProperty = DependencyProperty.Register(
            "EnableMouseClicksRecording", typeof(bool), typeof(HotkeySequenceEditor), new PropertyMetadata(true));
        
        public bool HideKeypressDelays
        {
            get { return (bool) GetValue(HideKeypressDelaysProperty); }
            set { SetValue(HideKeypressDelaysProperty, value); }
        }
        public bool EnableMousePositionRecording
        {
            get { return (bool) GetValue(EnableMousePositionRecordingProperty); }
            set { SetValue(EnableMousePositionRecordingProperty, value); }
        }

        public bool EnableKeyboardRecording
        {
            get { return (bool) GetValue(EnableKeyboardRecordingProperty); }
            set { SetValue(EnableKeyboardRecordingProperty, value); }
        }

        public bool EnableMouseClicksRecording
        {
            get { return (bool) GetValue(EnableMouseClicksRecordingProperty); }
            set { SetValue(EnableMouseClicksRecordingProperty, value); }
        }
        
        public int MaxItemsCount
        {
            get { return (int) GetValue(MaxItemsCountProperty); }
            set { SetValue(MaxItemsCountProperty, value); }
        }

        public TimeSpan MaxRecordingDuration
        {
            get { return (TimeSpan) GetValue(MaxRecordingDurationProperty); }
            set { SetValue(MaxRecordingDurationProperty, value); }
        }
        public TimeSpan MousePositionRecordingResolution
        {
            get { return (TimeSpan) GetValue(MousePositionRecordingResolutionProperty); }
            set { SetValue(MousePositionRecordingResolutionProperty, value); }
        }
        
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
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