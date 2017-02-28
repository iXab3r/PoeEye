using System;
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
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                supplierCompletionSource.SetResult(allEntities);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                supplierCompletionSource.TrySetResult(new string[0]);
            }
        }
    }
}