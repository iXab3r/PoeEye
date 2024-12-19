using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public class TimelineItemBase<T> : BlazorReactiveComponent<T> where T : TimelineEntry 
{
    public TimelineItemBase()
    {
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.ProgressPercent));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.Text));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.Timestamp));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.IsBusy));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.PrefixIcon));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.DataContext.Images));
    }
}