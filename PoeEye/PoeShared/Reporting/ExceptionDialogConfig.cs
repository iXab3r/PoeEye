using System;
using JetBrains.Annotations;
using PoeShared.Reporting;

namespace PoeShared.UI;

/// <summary>
/// Describes one problem report and its dialog. The reporting service supplies host defaults
/// for omitted values; the caller owns the initial comment.
/// </summary>
public sealed record ExceptionDialogConfig
{
    public string Title { [CanBeNull] get; [CanBeNull] init; }
        
    public string AppName { [CanBeNull] get; [CanBeNull] init; }

    /// <summary>
    /// Gets the initial comment, which the user can edit before saving or sending the report.
    /// </summary>
    public string Comment { [CanBeNull] get; [CanBeNull] init; }

    public DateTimeOffset Timestamp { get; init; }
        
    public Exception Exception { get; init; }
        
    public IErrorReportHandler ReportHandler { get; init; }
    
    public ExceptionReportItem[] ReportItems { get; init; }
        
    public IErrorReportItemProvider[] ItemProviders { get; init; }

    public bool IsValid => !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(AppName);
}
