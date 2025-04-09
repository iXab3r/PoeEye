using System;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using DynamicData;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

/// <summary>
/// Reactive version of CompositeFileProvider
/// </summary>
internal sealed class ComplexFileProvider : DisposableReactiveObjectWithLogger, IFileProvider
{
    private readonly ISourceList<IFileProvider> providersSource = new SourceList<IFileProvider>();

    public ComplexFileProvider()
    {
        providersSource
            .Connect()
            .Subscribe(x =>
            {
                FileProviders = providersSource.Items.ToImmutableArray();   
            })
            .AddTo(Anchors);
    }

    public ImmutableArray<IFileProvider> FileProviders { get; private set; } = ImmutableArray<IFileProvider>.Empty;

    public IDisposable Add(IFileProvider fileProvider)
    {
        providersSource.Add(fileProvider);
        return Disposable.Create(() =>
        {
            providersSource.Remove(fileProvider);
        });
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        foreach (var provider in FileProviders)
        {
            var fileInfo = provider.GetFileInfo(subpath);
            if (fileInfo is not NotFoundFileInfo)
            {
                return fileInfo;
            }
        }
        
        return new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        foreach (var provider in FileProviders)
        {
            var directory = provider.GetDirectoryContents(subpath);
            if (directory is not NotFoundDirectoryContents)
            {
                return directory;
            }
        }
        return NotFoundDirectoryContents.Singleton;
    }
    
    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }
}