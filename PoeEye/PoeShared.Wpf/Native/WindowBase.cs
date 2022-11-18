﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public class WindowBase : TransparentWindow
{
    public WindowBase()
    {
        using var sw = new BenchmarkTimer("View initialization", Log, nameof(OverlayWindowView));
        sw.Step("Components initialized");
        sw.Step("WhenLoaded routine executed");
        LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object sender, EventArgs e)
    {
        Log.Debug(() => $"Window location changed");
    }

    public IObservable<EventPattern<EventArgs>> WhenRendered => Observable
        .FromEventPattern<EventHandler, EventArgs>(h => ContentRendered += h, h => ContentRendered -= h);

    public override string ToString()
    {
        return $"{base.ToString()} DataContext: {DataContext.Dump()} {{X={Left:F0},Y={Top:F0},Width={Width:F0},Height={Height:F0}}}";
    }
}