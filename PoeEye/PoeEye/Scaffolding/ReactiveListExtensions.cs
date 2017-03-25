using DynamicData;
using DynamicData.ReactiveUI;
using Guards;
using ReactiveUI;

namespace PoeEye.Scaffolding
{
    internal static class ReactiveListExtensions
    {
        public static ISourceList<T> ToSourceList<T>(this ReactiveList<T> list)
        {
            Guard.ArgumentNotNull(() => list);

            return new SourceList<T>(list.ToObservableChangeSet());
        }
    }
}