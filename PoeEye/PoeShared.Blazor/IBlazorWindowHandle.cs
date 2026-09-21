using System;

namespace PoeShared.Blazor;

/// <summary>
/// Identity of the native window hosting a component. Registered in that window's DI scope,
/// allowing portable components to pass an explicit dialog owner without referencing WPF.
/// Browser-only hosts may omit this service.
/// </summary>
public interface IBlazorWindowHandle
{
    IntPtr Handle { get; }
}
