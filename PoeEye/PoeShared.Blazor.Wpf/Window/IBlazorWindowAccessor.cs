namespace PoeShared.Blazor.Wpf;

/// <summary>
/// This interface could be used to access current Blazor Window from inside its context
/// </summary>
public interface IBlazorWindowAccessor
{
    IBlazorWindow Window { get; }
}