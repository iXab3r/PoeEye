using System.Reactive.Concurrency;
using System.Threading;
using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface ISchedulerProvider
{
    [NotNull]
    IScheduler GetOrCreate([NotNull] string name);
    
    [NotNull]
    IScheduler Create([NotNull] string name, ThreadPriority threadPriority);
}