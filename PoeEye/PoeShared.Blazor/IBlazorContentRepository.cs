using DynamicData;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor;

public interface IBlazorContentRepository
{
    ISourceList<IFileInfo> AdditionalFiles { get; }
}