using System;
using System.IO;
using JetBrains.Annotations;

namespace PoeShared.UI.Models
{
    public interface IImagesCacheService
    {
        [NotNull] 
        IObservable<FileInfo> ResolveImageByUri([NotNull] Uri imageUri);
    }
}