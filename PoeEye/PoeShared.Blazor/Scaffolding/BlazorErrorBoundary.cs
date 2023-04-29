using System;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Scaffolding;

public class BlazorErrorBoundary : ErrorBoundary
{
    public new Exception CurrentException => base.CurrentException;
}