using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Scaffolding;

public static class FileProviderExtensions
{
    public static IObservable<FileSystemEventArgs> Watch(this PhysicalFileProvider fileProvider, string fileName)
    {
        return Observable.Create<FileSystemEventArgs>(observer =>
        {
            var anchors = new CompositeDisposable();

            var changeToken = fileProvider.Watch(fileName);

            var filePath = Path.Combine(fileProvider.Root, fileName);
            
            changeToken.RegisterChangeCallback(_ =>
            {
                var fileEvent = new FileSystemEventArgs(WatcherChangeTypes.Changed, fileProvider.Root, fileName);
                observer.OnNext(fileEvent);
            }, state: null);
            
            return anchors;
        });
    }
}