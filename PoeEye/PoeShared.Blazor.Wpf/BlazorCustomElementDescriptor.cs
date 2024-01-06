using System;

namespace PoeShared.Blazor.Wpf;

public readonly record struct BlazorCustomElementDescriptor(Type ComponentType, string Identifier)
{
}