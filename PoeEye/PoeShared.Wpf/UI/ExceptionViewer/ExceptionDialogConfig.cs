using System;
using System.IO;
using JetBrains.Annotations;

namespace PoeShared.UI;

public sealed record ExceptionDialogConfig
{
    public string Title { [CanBeNull] get; [CanBeNull] init; }
        
    public string AppName { [CanBeNull] get; [CanBeNull] init; }
        
    public DateTimeOffset Timestamp { get; init; }
        
    public Exception Exception { get; init; }
        
    public IExceptionReportHandler ReportHandler { get; init; }
        
    public IExceptionReportItemProvider[] ItemProviders { get; init; }
}

public sealed record ExceptionReportItem
{
    public string Description { get; init; }

    public bool Attached { get; init; } = true;
        
    public FileInfo Attachment { get; init; }
}