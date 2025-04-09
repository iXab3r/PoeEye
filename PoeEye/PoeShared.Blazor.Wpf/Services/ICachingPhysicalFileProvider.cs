using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Wpf.Services;

/// <summary>
/// This file provider caches pre-loads all files inside content root which measurably reduces access time on slow systems.
/// Also this fixes a problem with virtualized file system - file watchers which are used by PhysicalFileProvider are not stable
/// </summary>
internal interface ICachingPhysicalFileProvider : IFileProvider
{
}