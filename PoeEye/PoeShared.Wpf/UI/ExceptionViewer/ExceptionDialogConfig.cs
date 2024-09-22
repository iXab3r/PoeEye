using System;
using JetBrains.Annotations;
using PoeShared.Reporting;

namespace PoeShared.UI;

public sealed record ExceptionDialogConfig
{
    public string Title { [CanBeNull] get; [CanBeNull] init; }
        
    public string AppName { [CanBeNull] get; [CanBeNull] init; }
        
    public DateTimeOffset Timestamp { get; init; }
        
    public Exception Exception { get; init; }
        
    public IErrorReportHandler ReportHandler { get; init; }
    
    public ExceptionReportItem[] ReportItems { get; init; }
        
    public IErrorReportItemProvider[] ItemProviders { get; init; }

    public bool IsValid => !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(AppName);
}