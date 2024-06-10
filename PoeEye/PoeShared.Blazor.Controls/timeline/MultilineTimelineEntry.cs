using DynamicData;

namespace PoeShared.Blazor.Controls;

public sealed class MultilineTimelineEntry : TimelineEntry
{
    public bool IsHtml { get; set; }

    public ISourceList<string> TextLines { get; } = new SourceList<string>();
}