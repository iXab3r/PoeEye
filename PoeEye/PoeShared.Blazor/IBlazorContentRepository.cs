using DynamicData;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor;

public interface IBlazorContentRepository : IJSComponentConfiguration
{
    ISourceList<IFileInfo> AdditionalFiles { get; }
    
}