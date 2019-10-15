using System;
using System.ComponentModel;
using System.Reactive.Linq;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding
{
    public static class NotifyPropertyChangedExtensions
    {
        /// <summary>
        /// Notifies when any any property on the object has changed
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static IObservable<string> WhenPropertyChanged<TObject>([NotNull] this TObject source)
            where TObject : INotifyPropertyChanged
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
                (
                    handler => source.PropertyChanged += handler,
                    handler => source.PropertyChanged -= handler
                )
                .Select(x => x.EventArgs.PropertyName);
        }        
    }
}