using System;

namespace PoeShared.Blazor.Controls;

public class ErrorTimelineEntry : TimelineEntry
{
    public Exception Exception { get; }

    public ErrorTimelineEntry(Exception exception)
    {
        Exception = exception;
        Text = "Error occurred";
    }
}