using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;

namespace PoeShared.Blazor.Wpf;

public interface ICoreWebView2Accessor
{
    CoreWebView2 CoreWebView2 { get; }
}