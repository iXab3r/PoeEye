using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualBasic.Logging;
using PoeShared.Prism;
using ReactiveUI;

namespace PoeShared.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            RxApp.MainThreadScheduler = new DispatcherScheduler(dispatcher, DispatcherPriority.Normal);;
            RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }
}