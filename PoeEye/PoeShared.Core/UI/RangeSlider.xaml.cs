using System;
using System.Windows;
using System.Windows.Controls;

namespace PoeShared.UI
{
    public partial class ColorRangeSlider : UserControl
    {
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<double[]>),
            typeof(ColorRangeSlider));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(ColorRangeSlider), new UIPropertyMetadata(0d));

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register("LowerValue", typeof(double), typeof(ColorRangeSlider), new UIPropertyMetadata(0d));

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register("UpperValue", typeof(double), typeof(ColorRangeSlider), new UIPropertyMetadata(0d));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(ColorRangeSlider), new UIPropertyMetadata(1d));

        public ColorRangeSlider()
        {
            InitializeComponent();

            Loaded += RangeSlider_Loaded;
        }

        public double Minimum
        {
            get => (double) GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double LowerValue
        {
            get => (double) GetValue(LowerValueProperty);
            set => SetValue(LowerValueProperty, value);
        }

        public double UpperValue
        {
            get => (double) GetValue(UpperValueProperty);
            set => SetValue(UpperValueProperty, value);
        }

        public double Maximum
        {
            get => (double) GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private void RangeSlider_Loaded(object sender, RoutedEventArgs e)
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;
        }

        public event RoutedPropertyChangedEventHandler<double[]> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        private void LowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpperSlider.Value = Math.Max(UpperSlider.Value, LowerSlider.Value);
            OnValueChanged(new[] {e.OldValue, UpperValue}, new[] {e.NewValue, UpperValue});
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LowerSlider.Value = Math.Min(UpperSlider.Value, LowerSlider.Value);
            OnValueChanged(new[] {LowerValue, e.OldValue}, new[] {LowerValue, e.NewValue});
        }

        protected virtual void OnValueChanged(double[] oldValue, double[] newValue)
        {
            var changedEventArgs = new RoutedPropertyChangedEventArgs<double[]>(oldValue, newValue) {RoutedEvent = ValueChangedEvent};
            RaiseEvent(changedEventArgs);
        }
    }
}