using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Wpf;

public interface IBlazorContentControl
{
    Type ViewType { get; set; }
    object Content { get; set; }
    IEnumerable<IFileInfo> AdditionalFiles { get; set; }
    bool IsBusy { get; }

    /// <summary>
    ///     We have to dynamically recreate WebView when needed as it is EXTREMELY unfriendly for any changes of associated
    ///     properties
    /// </summary>
    BlazorWebViewEx WebView { get; }

    Exception UnhandledException { get; }
    ICommand ReloadCommand { get; }
    ICommand OpenDevTools { get; }
    bool Focus();
}