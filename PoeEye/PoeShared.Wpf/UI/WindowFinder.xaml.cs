using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using PInvoke;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Cursor = System.Windows.Input.Cursor;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

// ReSharper disable CommentTypo
// ReSharper disable RedundantExplicitArrayCreation

namespace PoeShared.UI;

public partial class WindowFinder
{
    public static readonly DependencyProperty PickCommandProperty = DependencyProperty.Register(
        nameof(PickCommand), typeof(ICommand), typeof(WindowFinder), new PropertyMetadata(default(ICommand)));
    
    public static readonly DependencyProperty MinimizeActiveWindowProperty = DependencyProperty.Register(
        nameof(MinimizeActiveWindow), typeof(bool), typeof(WindowFinder), new PropertyMetadata(default(bool)));
    
    private static readonly WpfPoint CursorHotSpot = new(16, 20);
    
    private readonly SerialDisposable activeSearchAnchors;
    private readonly ConcurrentDictionary<IntPtr, IntPtr> parentByWindow = new();
    
    private Cursor crosshairCursor;
    private WpfPoint startPoint;
    private WindowFinderWindowInfo currentWindowInfo;
    
    private Cursor currentWindowInfoCursor;
    private Cursor lastWindowInfoCursor;
    
    public WindowFinder()
    {
        activeSearchAnchors = new SerialDisposable();
        InitializeComponent();
        Loaded += (_, _) => crosshairCursor = WindowInfoControl.ConvertToCursor(CursorHotSpot);
    }

    public WindowInfoControl WindowInfoControl { get; } = new();

    public ICommand PickCommand
    {
        get => (ICommand) GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }
    
    public bool MinimizeActiveWindow
    {
        get => (bool) GetValue(MinimizeActiveWindowProperty);
        set => SetValue(MinimizeActiveWindowProperty, value);
    }
    
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(null);
        CaptureMouse();
        e.Handled = true;
        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (e.LeftButton != MouseButtonState.Pressed || activeSearchAnchors.Disposable != null)
        {
            return;
        }

        var currentPosition = e.GetPosition(null);
        var diff = startPoint - currentPosition;
        if ((Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            StartSearch();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        var windowInfoToUse = currentWindowInfo;

        StopSearch();

        if (windowInfoToUse is null)
        {
            return;
        }

        var match = new WindowFinderMatch
        {
            Window = windowInfoToUse.WindowHandle,
            CursorLocation = UnsafeNative.GetCursorPosition(),
        };
        PickCommand?.Execute(match);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            StopSearch();
        }

        base.OnPreviewKeyDown(e);
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        var windowUnderCursor = UnsafeNative.GetWindowUnderCursor();
        if (windowUnderCursor == IntPtr.Zero)
        {
            return;
        }

        if (currentWindowInfo != null)
        {
            if (currentWindowInfo.WindowHandle.Handle == windowUnderCursor)
            {
                return;
            }
        }

        var window = new WindowHandle(windowUnderCursor);
        if (window.ParentProcessId == Environment.ProcessId || window.ProcessId == Environment.ProcessId)
        {
            return;
        }
        
        var parentWindow = parentByWindow.GetOrAdd(windowUnderCursor, UnsafeNative.FindRootWindow);
        var windowHandle = parentWindow == window.Handle ? window : new WindowHandle(parentWindow);
        currentWindowInfo = new WindowFinderWindowInfo(windowHandle);
        WindowInfoControl.DataContext = currentWindowInfo;
        UpdateCursor();
    }

    private void StartSearch()
    {
        var foregroundWindow = UnsafeNative.GetForegroundWindow();
        CaptureMouse();
        Keyboard.Focus(StartSearchButton);
        parentByWindow.Clear();
        CrosshairImage.Visibility = Visibility.Hidden;
        currentWindowInfo = null;
        startPoint = default;

        var shouldMinimize = MinimizeActiveWindow;
        bool shouldRestore;
        if (foregroundWindow != IntPtr.Zero && shouldMinimize)
        {
            shouldRestore = User32.SetWindowPos(
                foregroundWindow, 
                UnsafeNative.HWND_BOTTOM, 0, 0, 0, 0, 
                User32.SetWindowPosFlags.SWP_NOSIZE | 
                User32.SetWindowPosFlags.SWP_NOACTIVATE | 
                User32.SetWindowPosFlags.SWP_NOMOVE);
        }
        else
        {
            shouldRestore = false;
        }

        var searchAnchors = new CompositeDisposable().AssignTo(activeSearchAnchors);
        Observable
            .FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => MouseMove += h,
                h => MouseMove -= h)
            .Select(x => x.EventArgs)
            .Subscribe(HandleMouseMove)
            .AddTo(searchAnchors);
        searchAnchors.Add(() =>
        {
            ReleaseMouseCapture();
            parentByWindow.Clear();
            currentWindowInfo = null;
            CrosshairImage.Visibility = Visibility.Visible;
            UpdateCursor();

            if (foregroundWindow != IntPtr.Zero && shouldRestore)
            {
                User32.SetWindowPos(
                    foregroundWindow, 
                    UnsafeNative.HWND_TOP, 0, 0, 0, 0, 
                    User32.SetWindowPosFlags.SWP_NOSIZE | 
                    User32.SetWindowPosFlags.SWP_NOACTIVATE | 
                    User32.SetWindowPosFlags.SWP_NOMOVE);
            }
        });
        
        UpdateCursor();
    }

    private void StopSearch()
    {
        activeSearchAnchors.Disposable = null;
    }

    private void UpdateCursor()
    {
        if (currentWindowInfo != null)
        {
            lastWindowInfoCursor = currentWindowInfoCursor;
            currentWindowInfoCursor = WindowInfoControl.ConvertToCursor(CursorHotSpot);
            Cursor = currentWindowInfoCursor;
        }
        else if (activeSearchAnchors.Disposable != null)
        {
            Cursor = crosshairCursor;
        }
        else
        {
            Cursor = null;
        }

        lastWindowInfoCursor?.Dispose();
        lastWindowInfoCursor = null;
    }

}