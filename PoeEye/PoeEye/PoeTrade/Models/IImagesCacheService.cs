namespace PoeEye.PoeTrade.Models
{
    using System;
    using System.IO;

    using JetBrains.Annotations;

    internal interface IImagesCacheService
    {
        [NotNull] 
        IObservable<FileInfo> ResolveImageByUri([NotNull] Uri imageUri);
    }
}