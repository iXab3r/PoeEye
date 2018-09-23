using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Common.Logging;
using Guards;
using PoeEye.PoeTrade.Shell.ViewModels;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.Shell.Services
{
    internal sealed class SplashWindow : DisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SplashWindow));

        private readonly Window mainWindow;
        private Window splashWindow;

        public SplashWindow(Window mainWindow)
        {
            Guard.ArgumentNotNull(mainWindow, nameof(mainWindow));

            this.mainWindow = mainWindow;

            var thread = new Thread(() =>
            {
                try
                {
                    Log.Debug("Splash window thread started");

                    var dispatcher = Dispatcher.CurrentDispatcher;
                    
                    Disposable.Create(() =>
                    {
                        Log.Debug("Shutting down Dispatcher");
                        dispatcher.InvokeShutdown();
                        Log.Debug("Dispatcher has been shut down");
                    }).AddTo(Anchors);

                    dispatcher.BeginInvoke((Action)(() =>
                    {
                        splashWindow = new Window
                        {
                            Content = new SplashWindowViewModel(),
                            Background = null,
                            AllowsTransparency = true,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ShowActivated = true,
                            ShowInTaskbar = true,
                            WindowStyle = WindowStyle.None,
                            ResizeMode = ResizeMode.NoResize,
                            SizeToContent = SizeToContent.WidthAndHeight
                        };

                        Log.Debug("Showing splash window...");
                        splashWindow.ShowDialog();
                        Log.Debug("Splash window closed");
                    }));
                    Dispatcher.Run();
                }
                catch (Exception e)
                {
                    Log.Error("Error", e);
                }
                finally
                {
                    Log.Debug("Splash window thread terminated");
                }
            });
            thread.Name = "SplashWindow";
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Close()
        {
            Log.Debug("Closing splash and opening Main window");
            CloseWindowSafe(splashWindow);

            mainWindow.Opacity = 1;
            mainWindow.ShowInTaskbar = true;

            Dispose();
        }

        public void Show()
        {
            Log.Debug("Initializing Main window");

            mainWindow.AllowsTransparency = true;
            mainWindow.ShowInTaskbar = false;
            mainWindow.ShowActivated = true;
            mainWindow.Opacity = 0;
            mainWindow.Show();
            mainWindow.Activate();
        }

        private static void CloseWindowSafe(Window window)
        {
            if (window == null)
            {
                return;
            }
            if (window.Dispatcher.CheckAccess())
            {
                window.Close();
            }
            else
            {
                window.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(window.Close));
            }
        }
    }
}