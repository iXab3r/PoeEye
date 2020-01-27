using System;
using System.ComponentModel;
using System.Reactive;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;


namespace PoeShared.Scaffolding
{
    public static class ChangeSetExtensions
    {
        public static ISourceList<T> ToSourceList<T>(this IObservable<IChangeSet<T>> source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            return new SourceList<T>(source);
        }
        
        /// <summary>
        /// Watches each item in the collection and notifies when any of them has changed
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="propertiesToMonitor">specify properties to Monitor, or omit to monitor all property changes</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenPropertyChanged<TObject>([NotNull] this IObservable<IChangeSet<TObject>> source, params string[] propertiesToMonitor)
            where TObject : INotifyPropertyChanged
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.MergeMany(t => t.WhenAnyProperty(propertiesToMonitor));
        }
    }
}