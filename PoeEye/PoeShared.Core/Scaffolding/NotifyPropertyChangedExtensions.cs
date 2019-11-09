using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public static class NotifyPropertyChangedExtensions
    {
        public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject>([NotNull] this TObject source, params string[] propertiesToMonitor)
            where TObject : INotifyPropertyChanged
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    handler => source.PropertyChanged += handler,
                    handler => source.PropertyChanged -= handler
                )
                .Where(x => propertiesToMonitor == null || propertiesToMonitor.Length == 0 || propertiesToMonitor.Contains(x.EventArgs.PropertyName));
        }
    }
}