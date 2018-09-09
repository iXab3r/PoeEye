using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.PoeDatabase
{
    internal sealed class ComplexPoeDatabaseReader : DisposableReactiveObject, IPoeDatabaseReader
    {
        private readonly IPoeDatabaseReader[] readers;
        private readonly ReadOnlyObservableCollection<string> knownEntityNames;

        public ComplexPoeDatabaseReader(
            [NotNull] IPoeDatabaseReader[] readers,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(readers, nameof(readers));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            this.readers = readers;

            var merge = new SourceList<ISourceList<string>>();

            foreach (var poeDatabaseReader in readers)
            {
                var changeSet = poeDatabaseReader
                    .KnownEntityNames
                    .ToObservableChangeSet();
                var sourceList = new SourceList<string>(changeSet);

                merge.Add(sourceList);
            }

            merge
                .Or()
                .ObserveOn(uiScheduler)
                .Bind(out knownEntityNames)
                .Subscribe()
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<string> KnownEntityNames => knownEntityNames;

        private string[] CleanupEntities(string[] source)
        {
            var result = new List<string>();

            var lastEntity = default(string);
            foreach (var entity in source)
            {
                if (!string.IsNullOrWhiteSpace(lastEntity) && entity.StartsWith(lastEntity))
                {
                    continue;
                }

                lastEntity = entity;
                result.Add(lastEntity);
            }

            return result.ToArray();
        }
    }
}
