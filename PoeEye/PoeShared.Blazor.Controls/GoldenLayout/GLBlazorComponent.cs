using System;

namespace PoeShared.Blazor.Controls.GoldenLayout;

public readonly record struct GLBlazorComponent(
    GLBlazorComponentState ComponentState, 
    object? DataContext, 
    Type? BodyViewType,
    Type? HeaderViewType,
    object? BodyViewTemplateKey = null, 
    object? HeaderViewTemplateKey = null)
{
}
