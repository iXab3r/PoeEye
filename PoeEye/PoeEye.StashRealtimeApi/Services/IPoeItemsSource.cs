using System;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeEye.StashRealtimeApi.Services
{
    internal interface IPoeItemsSource : IDisposable
    {
        IObservable<IPoeItem[]> ItemPacks { [NotNull] get; }
    }
}