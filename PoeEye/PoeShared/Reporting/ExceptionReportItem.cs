namespace PoeShared.Reporting;

public sealed record ExceptionReportItem
{
    public string Description { get; init; }

    public bool Attached { get; init; } = true;

    public bool IsRequired { get; init; }
        
    public FileInfo Attachment { get; init; }
}