using System.Reactive.Subjects;

namespace PoeShared.Blazor;

public interface IRefreshableComponent
{
    /// <summary>
    /// Two-side channel which allows no notify component when it has to be refreshed AND allows the component model to notify about refresh request 
    /// </summary>
    ISubject<object> WhenRefresh { get; }
}