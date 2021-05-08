using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Prism.Events;

namespace PoeShared.Wpf.Scaffolding
{
    public static class EventAggregatorExtensions
    {
        public static IObservable<T> ToObservable<T>(this PubSubEvent<T> pubSubEvent, ThreadOption threadOption = ThreadOption.PublisherThread)
        {
            return Observable.Create<T>(observer =>
            {
                var anchors = new CompositeDisposable();

                var subscription = pubSubEvent
                    .Subscribe(observer.OnNext, threadOption);
                anchors.Add(subscription);

                return anchors;
            });
        }
    }
}