﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using log4net;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    public partial class OverlayWindowView
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayWindowView));

        public OverlayWindowView()
        {
            using var sw = new BenchmarkTimer("View initialization", Log, nameof(OverlayWindowView));
            InitializeComponent();
            sw.Step("Components initialized");
            WhenLoaded.Subscribe(OnLoaded);
            sw.Step("WhenLoaded routine executed");
            SizeChanged += OnSizeChanged;
        }

        public IObservable<EventPattern<RoutedEventArgs>> WhenLoaded => Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => Loaded += h, h => Loaded -= h);

        public IObservable<EventPattern<EventArgs>> WhenRendered => Observable
            .FromEventPattern<EventHandler, EventArgs>(h => ContentRendered += h, h => ContentRendered -= h);

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            var window = sender as Window;
            var windowViewModel = window?.DataContext as OverlayWindowViewModel;
            var overlayViewModel = windowViewModel?.Content as OverlayViewModelBase;
            if (overlayViewModel == null)
            {
                return;
            }

            var delta = sizeChangedEventArgs.NewSize.Height - sizeChangedEventArgs.PreviousSize.Height;
            if (!overlayViewModel.GrowUpwards)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() => Top -= delta), DispatcherPriority.Render);
        }
        
        private void OnLoaded()
        {
            var helper = new WindowInteropHelper(this);
            WindowsServices.SetWindowExNoActivate(helper.Handle);
        }

        public override string ToString()
        {
            return $"[OverlayWindow] DataContext: {DataContext} (X:{Left} Y:{Top} Width: {Width} Height: {Height})";
        }

        public void SetOverlayMode(OverlayMode mode)
        {
            if (AllowsTransparency == false && mode == OverlayMode.Transparent)
            {
                throw new InvalidOperationException($"Transparent mode requires AllowsTransparency to be set to True");
            }
            switch (mode)
            {
                case OverlayMode.Layered:
                    MakeLayered();
                    break;
                case OverlayMode.Transparent:
                    MakeTransparent();
                    break;
            }
        }
        
        private void OverlayChildWindow_OnSizeChanged(object sender, SizeChangedEventArgs sizeInfo)
        {
            var window = sender as Window;
            var windowViewModel = window?.DataContext as OverlayWindowViewModel;
            var overlayViewModel = windowViewModel?.Content as OverlayViewModelBase;
            if (window == null || windowViewModel == null || overlayViewModel == null || sizeInfo == null)
            {
                return;
            }

            try
            {
                if (sizeInfo.HeightChanged)
                {
                    overlayViewModel.ActualHeight = sizeInfo.NewSize.Height;
                }

                if (sizeInfo.WidthChanged)
                {
                    overlayViewModel.ActualWidth = sizeInfo.NewSize.Width;
                }
            }
            catch (Exception exception)
            {
                Log.HandleUiException(exception);
            }
        }
    }
    
}