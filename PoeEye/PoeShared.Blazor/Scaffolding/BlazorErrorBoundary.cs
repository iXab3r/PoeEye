using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Scaffolding;

public class BlazorErrorBoundary : ErrorBoundary
{
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    public new Exception CurrentException => base.CurrentException;

    protected override Task OnErrorAsync(Exception exception)
    {
        return OnError.HasDelegate
            ? OnError.InvokeAsync(exception)
            : Task.CompletedTask;
    }
}
