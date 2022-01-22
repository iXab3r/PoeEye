using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoeShared.UI;

/// <summary>
/// Interaction logic for AdornerTest.xaml
/// </summary>
public partial class AdornerTest : UserControl
{
    public AdornerTest()
    {
        InitializeComponent();
    }

    private void DoBlockingTask(object sender, RoutedEventArgs e)
    {
        busy.IsBusyIndicatorShowing = true;
        Thread.Sleep(5000);
        busy.IsBusyIndicatorShowing = false;
    }

    private void DoBlockingTaskWithFadeOut(object sender, RoutedEventArgs e)
    {
        // set the FadeTime to 0 seconds to ensure that it is as immediate as possible
        busy.FadeTime = TimeSpan.Zero;
        busy.IsBusyIndicatorShowing = true;
        // in order for setting the opacity to take effect, you have to delay the task slightly to ensure WPF has time to process the updated visual
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Thread.Sleep(5000);
            busy.IsBusyIndicatorShowing = false;
            busy.ClearValue(BusyDecorator.FadeTimeProperty);
        }), DispatcherPriority.Background);
    }

    private void DoBackgroundTask(object sender, RoutedEventArgs e)
    {
        busy.IsBusyIndicatorShowing = true;
        var task = Task.Factory.StartNew(() => Thread.Sleep(5000));
        task.ContinueWith(r => busy.IsBusyIndicatorShowing = false, TaskScheduler.FromCurrentSynchronizationContext());
    }
}