using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.FileProviders;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

public interface IBlazorContentControl : IBlazorHostController, IDisposableReactiveObject
{
    Type ViewType { get; set; }
    object Content { get; set; }
    IEnumerable<IFileInfo> AdditionalFiles { get; set; }
    bool IsBusy { get; }

    BlazorWebViewEx WebView { get; }

    Exception UnhandledException { get; }

    object FindResource(object resourceKey);

    event DragEventHandler DragEnter;
}
