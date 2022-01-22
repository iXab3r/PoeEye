using System.Reactive.Concurrency;
using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface ISchedulerProvider
{
    [NotNull]
    IScheduler GetOrCreate([NotNull] string name);
}