using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PoeShared.UI
{
    public partial class RangeSlider : UserControl
    {
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double[]>), typeof(RangeSlider));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(0d));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(1d));

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register("LowerValue", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register("UpperValue", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty TrackBackgroundProperty = DependencyProperty.Register(
            nameof(TrackBackground), typeof(Brush), typeof(RangeSlider), new PropertyMetadata(default(Brush)));

        public RangeSlider()
        {
            InitializeComponent();

            Loaded += RangeSlider_Loaded;
        }

        public Brush TrackBackground
        {
            get => (Brush) GetValue(TrackBackgroundProperty);
            set => SetValue(TrackBackgroundProperty, value);
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

        void RangeSlider_Loaded(object sender, RoutedEventArgs e)
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
            UpperSlider.SetCurrentValue(RangeBase.ValueProperty, Math.Max(UpperSlider.Value, LowerSlider.Value));
            OnValueChanged(new[] {e.OldValue, UpperValue}, new[] {e.NewValue, UpperValue});
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LowerSlider.SetCurrentValue(RangeBase.ValueProperty, Math.Min(UpperSlider.Value, LowerSlider.Value));
            OnValueChanged(new[] {LowerValue, e.OldValue}, new[] {LowerValue, e.NewValue});
        }

        protected virtual void OnValueChanged(double[] oldValue, double[] newValue)
        {
            var changedEventArgs = new RoutedPropertyChangedEventArgs<double[]>(oldValue, newValue) {RoutedEvent = ValueChangedEvent};
            RaiseEvent(changedEventArgs);
        }
    }
}