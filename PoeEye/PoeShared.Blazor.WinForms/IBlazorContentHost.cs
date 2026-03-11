#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Wpf;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.WinForms;

public interface IBlazorContentHost : IBlazorHostController, IDisposableReactiveObject
{
    Type? ViewType { get; set; }

    object? Content { get; set; }

    IEnumerable<IFileInfo>? AdditionalFiles { get; set; }

    bool IsBusy { get; }

    BlazorWebViewEx WebView { get; }

    Exception? UnhandledException { get; }
}
