using System;
using DynamicData;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor;

/// <summary>
/// There is a catch with IJSComponentConfiguration and JSComponents:
/// How it works:
/// - when page is loaded, payload with custom component metadata gets sent to the client and stored on JS side - attachWebRendererInterop - it contains Id of the component and some info about the actual type
/// - when JS side tries to resolve component, it interops back into .NET AddRootComponent and gets Id of a created component
/// - after that, it tries to resolve local (JS-side) metadata about the component to complete the registration process
/// - if all goes well, we now have a fully-functional component with custom rendering circuit
/// The problem is that if we add the component AFTER the page is loaded, we won't have the metadata available on the client(JS) side, so it won't be able to resolve it. 
/// </summary>
public interface IBlazorContentRepository : IJSComponentConfiguration
{
    ISourceList<IFileInfo> AdditionalFiles { get; }
}
