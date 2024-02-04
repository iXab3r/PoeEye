using DynamicData;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Prism;

namespace PoeShared.Blazor;

public sealed class BlazorContentRepository : IBlazorContentRepository
{
    private readonly ISourceList<IFileInfo> additionalFiles = new SourceList<IFileInfo>();

    public ISourceList<IFileInfo> AdditionalFiles => additionalFiles;

    public JSComponentConfigurationStore JSComponents { get; } = new();
}