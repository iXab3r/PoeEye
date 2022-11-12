﻿using System.Collections.Specialized;
using DynamicData;

namespace PoeShared.Scaffolding;

public interface ISourceListEx<T> : ISourceList<T>, IEnumerable<T>, IDisposableReactiveObject, INotifyCollectionChanged
{
    /// <summary>
    ///   Reactive collection that holds all items and provides notification, will have same items as Items if no scheduler is provided
    /// </summary>
    IReadOnlyObservableCollection<T> Collection { get; }
}