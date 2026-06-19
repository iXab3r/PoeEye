using System.Windows;

namespace PoeShared.UI;

public partial class ExceptionDialogView
{
    public ExceptionDialogView()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
