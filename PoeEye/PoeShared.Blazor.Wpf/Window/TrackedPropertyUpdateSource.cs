namespace PoeShared.Blazor.Wpf;

internal enum TrackedPropertyUpdateSource
{
    /// <summary>
    /// System-initiated changes (auto-resize, alignment, etc)
    /// </summary>
    Internal,  
    /// <summary>
    /// Direct user input (property setters, drag operations)
    /// </summary>
    External
}