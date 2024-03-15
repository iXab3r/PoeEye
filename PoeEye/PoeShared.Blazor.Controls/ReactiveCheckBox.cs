using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Controls;

partial class ReactiveCheckBox
{
    private static long globalElementIndex;
    private readonly long elementIndex = Interlocked.Increment(ref globalElementIndex);
    
    [Parameter]
    public bool IsChecked { get; set; }
    
    [Parameter] 
    public EventCallback<bool> IsCheckedChanged { get; set; }
    
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    public ReactiveCheckBox()
    {
        Class = "d-flex gap-1 align-self-center";
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.IsNullOrEmpty(Id))
        {
            Id = $"rcb-{elementIndex}";
        }
    }
    
    private Task HandleCheckedChanged(bool newValue)
    {
        return IsCheckedChanged.InvokeAsync(newValue);
    }
}