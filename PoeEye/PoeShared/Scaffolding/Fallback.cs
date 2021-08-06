﻿using System;
using System.Collections.Generic;
using PoeShared.Logging;
using PropertyBinder;
using ReactiveUI;
using System.Reactive.Linq;

namespace PoeShared.Scaffolding
{
    public sealed class Fallback<T> : DisposableReactiveObject
    {
        private static readonly IFluentLog Log = typeof(Fallback<T>).PrepareLogger();
        private static readonly Binder<Fallback<T>> Binder = new Binder<Fallback<T>>();
        private static readonly Func<T, T, bool> DefaultFallbackCondition = (_, __) => false;
        private readonly Func<T, T, bool> fallbackCondition;

        private T actualValue;

        private T defaultValue;

        private bool hasActualValue;

        static Fallback()
        {
            Binder.Bind(x => !EqualityComparer<T>.Default.Equals(default, x.ActualValue) && !x.fallbackCondition(x.ActualValue, x.DefaultValue)).To(x => x.HasActualValue);
        }

        public Fallback(Predicate<T> fallbackCondition) : this((actualValue, defaultValue) => fallbackCondition(actualValue))
        {
        }
        
        public Fallback(Func<T, T, bool> fallbackCondition)
        {
            this.fallbackCondition = fallbackCondition;
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ActualValue).ToUnit(),
                    this.WhenAnyValue(x => x.DefaultValue).ToUnit(),
                    this.WhenAnyValue(x => x.HasActualValue).ToUnit())
                .SubscribeSafe(() => RaisePropertyChanged(nameof(Value)), Log.HandleException)
                .AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }

        public Fallback() : this(fallbackCondition: DefaultFallbackCondition)
        {
        }

        public bool HasActualValue
        {
            get => hasActualValue;
            private set => RaiseAndSetIfChanged(ref hasActualValue, value);
        }

        public T Value
        {
            get => hasActualValue ? actualValue : defaultValue;
            set => ActualValue = value;
        }

        public T ActualValue
        {
            get => actualValue;
            set => RaiseAndSetIfChanged(ref actualValue, value);
        }

        public T DefaultValue
        {
            get => defaultValue;
            set => RaiseAndSetIfChanged(ref defaultValue, value);
        }

        public Fallback<T> SetValue(T value)
        {
            Value = value;
            return this;
        }

        public Fallback<T> SetDefaultValue(T value)
        {
            DefaultValue = value;
            return this;
        }

        public Fallback<T> ResetValue()
        {
            return SetValue(default(T));
        }

        public override string ToString()
        {
            return $"{Value} (default {DefaultValue}, hasValue: {HasActualValue})";
        }
    }
}