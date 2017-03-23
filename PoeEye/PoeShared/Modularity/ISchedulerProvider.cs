using System.Reactive.Concurrency;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface ISchedulerProvider
    {
        [NotNull]
        IScheduler GetOrCreate([NotNull] string name);
    }
}