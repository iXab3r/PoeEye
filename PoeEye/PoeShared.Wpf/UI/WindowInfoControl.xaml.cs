using System.Windows;

namespace PoeShared.UI;

public partial class WindowInfoControl
{
    public WindowInfoControl()
    {
        InitializeComponent();
        DataContextChanged += WindowInfoControl_DataContextChanged;
    }

    private void WindowInfoControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        WindowInfoContainer.Visibility = e.NewValue is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}