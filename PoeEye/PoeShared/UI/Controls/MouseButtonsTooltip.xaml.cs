using System.Windows;
using System.Windows.Controls;

namespace PoeShared.Scaffolding.WPF.Controls
{
    /// <summary>
    ///     Interaction logic for MouseButtonsTooltip.xaml
    /// </summary>
    public partial class MouseButtonsTooltip : UserControl
    {
        public static readonly DependencyProperty LeftButtonProperty = DependencyProperty.Register(
            "LeftButton", typeof(string), typeof(MouseButtonsTooltip), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty RightButtonProperty = DependencyProperty.Register(
            "RightButton", typeof(string), typeof(MouseButtonsTooltip), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty MouseToolTipProperty = DependencyProperty.Register(
            "MouseToolTip", typeof(object), typeof(MouseButtonsTooltip), new PropertyMetadata(default(object)));

        public MouseButtonsTooltip()
        {
            InitializeComponent();
        }

        public string LeftButton
        {
            get => (string)GetValue(LeftButtonProperty);
            set => SetValue(LeftButtonProperty, value);
        }

        public string RightButton
        {
            get => (string)GetValue(RightButtonProperty);
            set => SetValue(RightButtonProperty, value);
        }

        public object MouseToolTip
        {
            get => GetValue(MouseToolTipProperty);
            set => SetValue(MouseToolTipProperty, value);
        }
    }
}