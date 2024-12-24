using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

internal partial class BlazorWindow
{
    private sealed record PropertyValueHolder<TValue> : DisposableReactiveRecord
    {
        private readonly BehaviorSubject<PropertyState<TValue>> stateSubject;

        public PropertyValueHolder(
            BlazorWindow owner,
            string propertyToRaise)
        {
            PropertyToRaise = propertyToRaise;
            stateSubject = new BehaviorSubject<PropertyState<TValue>>(default)
                .AddTo(Anchors);

            stateSubject
                .Subscribe(x => State = x)
                .AddTo(Anchors);

            stateSubject
                .Where(x => x.UpdateSource is TrackedPropertyUpdateSource.Internal)
                .Subscribe(x => owner.RaisePropertyChanged(PropertyToRaise))
                .AddTo(Anchors);
        }

        public PropertyState<TValue> State { get; private set; }

        public string PropertyToRaise { get; }

        public IObservable<PropertyState<TValue>> Listen()
        {
            return Anchors.IsDisposed ? Observable.Return(State) : stateSubject;
        }

        public PropertyState<TValue> SetValue(TValue value, TrackedPropertyUpdateSource updateSource)
        {
            if (Anchors.IsDisposed)
            {
                return State;
            }

            var currentState = State;
            if (currentState.UpdateSource == updateSource && EqualityComparer<TValue>.Default.Equals(value, currentState.Value))
            {
                return currentState;
            }

            var newState = new PropertyState<TValue>()
            {
                Value = value,
                UpdateSource = updateSource,
                Revision = currentState.Revision + 1
            };
            stateSubject.OnNext(newState);
            return newState;
        }
    }

    private readonly record struct PropertyState<TValue>
    {
        public required TValue Value { get; init; }
        public required TrackedPropertyUpdateSource UpdateSource { get; init; }
        public required long Revision { get; init; }
    }
}