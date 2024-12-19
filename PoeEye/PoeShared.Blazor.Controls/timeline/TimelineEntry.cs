using System;
using System.IO;
using System.Reactive.Disposables;
using DynamicData;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Controls;

public abstract class TimelineEntry : DisposableReactiveObject
{
    protected TimelineEntry()
    {
        Images = new SourceList<FileInfo>().AddTo(Anchors);
    }

    public DateTime? Timestamp { get; init; }
    
    public bool IsBusy { get; set; }
    
    public double? ProgressPercent { get; set; }
    
    public string? Text { get; set; }

    public ISourceList<FileInfo> Images { get; }
    
    public string? PrefixIcon { get; init; }

    public IDisposable Rent()
    {
        IsBusy = true;
        return Disposable.Create(() => IsBusy = false);
    }

    protected void AppendTextLine(string text)
    {
        var existing = Text;
        Text = string.IsNullOrEmpty(existing) ? text : existing + Environment.NewLine + text;
    }
}