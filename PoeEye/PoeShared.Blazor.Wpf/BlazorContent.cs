using System;

namespace PoeShared.Blazor.Wpf;

public partial class BlazorContent
{
    public Type ViewType { get; }

    public BlazorContent(Type viewType)
    {
        ViewType = viewType;
    }
}