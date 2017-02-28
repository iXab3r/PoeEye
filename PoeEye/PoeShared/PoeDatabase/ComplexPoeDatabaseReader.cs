using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.PoeDatabase
{
    internal sealed class ComplexPoeDatabaseReader : DisposableReactiveObject, IPoeDatabaseReader
    {
        private readonly IPoeDatabaseReader[] readers;
        private readonly TaskCompletionSource<string[]> supplierCompletionSource;

        public ComplexPoeDatabaseReader([NotNull] IPoeDatabaseReader[] readers)
        {
            this.readers = readers;
            Guard.ArgumentNotNull(() => readers);

            supplierCompletionSource = new TaskCompletionSource<string[]>();

            Task.Factory.StartNew(GetAllEntities).AddTo(Anchors);
        }

        public string[] KnownEntitiesNames => supplierCompletionSource.Task.Result;

        private void GetAllEntities()
        {
            Log.Instance.Debug("[ComplexPoeDatabaseReader] Traversing readers...");
            try
            {
                var allEntities = readers
                    .AsParallel()
                    .Select(x => x.KnownEntitiesNames)
                    .SelectMany(x => x)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Log.Instance.Debug($"[ComplexPoeDatabaseReader] Got {allEntities.Length} entities");

                var cleanedEntities = CleanupEntities(allEntities);
                Log.Instance.Debug($"[ComplexPoeDatabaseReader] Post-cleanup: {allEntities.Length} entities");
                supplierCompletionSource.SetResult(cleanedEntities);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                supplierCompletionSource.TrySetResult(new string[0]);
            }
        }

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