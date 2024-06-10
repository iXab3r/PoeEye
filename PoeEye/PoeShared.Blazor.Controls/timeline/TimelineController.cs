using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Controls;

public sealed class TimelineController 
{
    private readonly CircularSourceList<TimelineEntry> itemsSource;
    
    public TimelineController(CircularSourceList<TimelineEntry> itemsSource)
    {
        this.itemsSource = itemsSource;
    }
    
    public bool PerformUpdateOnNextCycle { get; set; }

    public void Cancel()
    {
        
    }

    public void Add(TimelineEntry entry)
    {
        itemsSource.Add(entry);
    }
}