using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity.Attributes;

namespace PoeShared.PoeDatabase
{
    internal sealed class StaticPoeDatabaseReader : DisposableReactiveObject, IPoeDatabaseReader
    {
        private readonly ISourceList<string> knownEntities = new SourceList<string>();
        private readonly ReadOnlyObservableCollection<string> knownEntityNames;

        public StaticPoeDatabaseReader(
            [NotNull] [Dependency(WellKnownSchedulers.Background)]
            IScheduler bgScheduler)
        {
            Log.Instance.Debug("[StaticPoeDatabaseReader..ctor] Created");

            knownEntities
                .Connect()
                .Bind(out knownEntityNames)
                .Subscribe()
                .AddTo(Anchors);

            Initialize();
        }

        public ReadOnlyObservableCollection<string> KnownEntityNames => knownEntityNames;

        private void Initialize()
        {
            var entities = GetEntities();
            knownEntities.Clear();
            knownEntities.AddRange(entities);
        }

        private static string[] GetEntities()
        {
            Log.Instance.Debug($"[StaticPoeDatabaseReader] Loading database...");

            var result = new HashSet<string>();
            var resources = new HashSet<string>
            {
                "PoeTradeUniques.lst"
            };

            foreach (var resource in resources)
            {
                var resourcePath = $"{nameof(PoeDatabase)}.{resource}";
                var resourceData = ResourceReader.ReadResourceAsString(Assembly.GetExecutingAssembly(), resourcePath);

                if (string.IsNullOrEmpty(resourceData))
                {
                    continue;
                }

                var items = resourceData.SplitTrim("\n");

                foreach (var item in items)
                {
                    result.Add(item);
                }
            }

            Log.Instance.Debug($"[StaticPoeDatabaseReader] Loaded {result.Count} entries");
            return result.ToArray();
        }
    }
}