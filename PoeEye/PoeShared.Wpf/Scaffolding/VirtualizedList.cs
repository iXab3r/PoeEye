using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.UI;
using ReactiveUI;
using Unity;

namespace PoeShared.Scaffolding;

public sealed class VirtualizedList<T, TContainer> : DisposableReactiveObjectWithLogger
    where TContainer : IVirtualizedListContainer<T> where T : class
{
    private readonly IScheduler uiScheduler;
    private readonly IFactory<TContainer> containerFactory;
    private readonly ISourceList<TContainer> containersSource = new SourceListEx<TContainer>();

    public VirtualizedList(
        IObservable<IChangeSet<T>> changeSetSource,
        IFactory<TContainer> containerFactory)
    {
        this.containerFactory = containerFactory;

        SubscribeAndCreate(Log, changeSetSource, containerFactory, containersSource).AddTo(Anchors);
        Containers = containersSource;
    }
    
    public IObservableList<TContainer> Containers { get; }

    private static IDisposable SubscribeAndCreate(
        IFluentLog log,
        IObservable<IChangeSet<T>> changeSetSource,
        IFactory<TContainer> containerFactory,
        ISourceList<TContainer> containerSource) 
    {
        void CleanupContainer(TContainer container)
        {
            container.Value = null;
        }

        TContainer CreateContainer(T model)
        {
            var container = containerFactory.Create();
            container.ValueType = model.GetType();
            return container;
        }

        void AssignContainerValue(TContainer container, T model)
        {
            container.Value = model;
        }

        TContainer FindContainer(T model, int desiredIdx = -1)
        {
            if (desiredIdx < 0)
            {
                return CreateContainer(model).AddTo(containerSource);
            }

            var realizedContainerIdx = 0;
            var containerIdx = 0;
            var modelType = model.GetType();

            foreach (var containerCandidate in containerSource.Items)
            {
                if (containerCandidate.Value != null)
                {
                    // this container is assigned
                    realizedContainerIdx++;
                }

                if (realizedContainerIdx == desiredIdx && modelType == containerCandidate.ValueType && containerCandidate.Value == null)
                {
                    return containerCandidate;
                }

                if (realizedContainerIdx > desiredIdx)
                {
                    var newContainer = CreateContainer(model);
                    containerSource.Insert(containerIdx, newContainer);
                    return newContainer;
                }

                containerIdx++;
            }

            return CreateContainer(model).AddTo(containerSource);
        }

        var anchors = new CompositeDisposable();
        containerSource.Items.ForEach(x => x.Value = null);
        changeSetSource
            .ForEachItemChange(x =>
            {
                var itemChange = new { x.Reason, x.Current, x.CurrentIndex, x.Previous, x.PreviousIndex };
                log.Debug($"Processing container collection item change: {itemChange}");
                try
                {
                    switch (x.Reason)
                    {
                        case ListChangeReason.Add:
                        {
                            var model = x.Current;
                            var container = FindContainer(model, x.CurrentIndex);
                            AssignContainerValue(container, model);
                        }
                            break;
                        case ListChangeReason.Remove:
                        {
                            var model = x.Current;
                            var container = containerSource.Items.FirstOrDefault(container => ReferenceEquals(model, container.Value));
                            if (container == null)
                            {
                                // container for that item is not loaded/created yet
                                return;
                            }

                            CleanupContainer(container);
                        }
                            break;
                        case ListChangeReason.Moved:
                        {
                            var model = x.Current;
                            var currentContainer = containerSource.Items.FirstOrDefault(container => ReferenceEquals(model, container.Value));
                            if (currentContainer == null)
                            {
                                // container for that item is not loaded/created yet
                                return;
                            }

                            CleanupContainer(currentContainer);
                            var container = FindContainer(model, x.CurrentIndex);
                            AssignContainerValue(container, model);
                        }
                            break;
                        case ListChangeReason.Clear:
                            containerSource.Items.ForEach(CleanupContainer);
                            break;
                        case ListChangeReason.Replace:
                        {
                            if (x.Previous.HasValue)
                            {
                                var previousModel = x.Previous.Value;
                                var previousContainer = containerSource.Items.FirstOrDefault(container => ReferenceEquals(previousModel, container.Value));
                                if (previousContainer != null)
                                {
                                    CleanupContainer(previousContainer);
                                }
                            }

                            var model = x.Current;
                            var container = FindContainer(model, x.CurrentIndex);
                            AssignContainerValue(container, model);
                        }
                            break;
                        default:
                        {
                            throw new ArgumentOutOfRangeException($"Unsupported list operation: {x.Reason}");
                        }
                    }

                    log.Debug($"Processed container collection item change: {itemChange}");
                }
                catch (Exception e)
                {
                    log.Error($"Failed to process list change: {itemChange}", e);
                    throw;
                }
            })
            .Subscribe()
            .AddTo(anchors);

        return anchors;
    }
}