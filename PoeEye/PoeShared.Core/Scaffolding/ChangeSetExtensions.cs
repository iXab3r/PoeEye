using System;
using DynamicData;
using Guards;

namespace PoeShared.Scaffolding
{
    public static class ChangeSetExtensions
    {
        public static ISourceList<T> ToSourceList<T>(this IObservable<IChangeSet<T>> source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            return new SourceList<T>(source);
        }
    }
}