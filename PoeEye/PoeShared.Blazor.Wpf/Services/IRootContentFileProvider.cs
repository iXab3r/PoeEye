using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Wpf.Services;

/// <summary>
/// Represents a specialized file provider that resolves static web assets (e.g., wwwroot)
/// from the application's root or nearby locations. 
/// This abstraction wraps IFileProvider but provides clearer intent for resolving root-level static content.
/// </summary>
internal interface IRootContentFileProvider : IFileProvider
{
}