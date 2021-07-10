using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using log4net;
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public abstract class DisposableReactiveObject : IDisposableReactiveObject
    {
        private readonly Subject<Exception> thrownExceptions = new Subject<Exception>();
        
        public CompositeDisposable Anchors { get; } = new CompositeDisposable();

        public IObservable<Exception> ThrownExceptions => thrownExceptions;

        public virtual void Dispose()
        {
            try
            {
                Anchors.Dispose();
            }
            catch (Exception e)
            {
                thrownExceptions.OnNext(e);
            }
        }

        public TRet RaiseAndSetIfChanged<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            try
            {
                if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
                {
                    return newValue;
                }
            }
            catch (Exception e)
            {
                thrownExceptions.OnNext(e);
                throw;
            }

            return RaiseAndSet(ref backingField, newValue, propertyName);
        }
        
        public TRet RaiseAndSet<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            try
            {
                RaisingPropertyChanging(propertyName);
                backingField = newValue;
                RaisePropertyChanged(propertyName);
                return newValue;
            }
            catch (Exception e)
            {
                thrownExceptions.OnNext(e);
                throw;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void RaisePropertyChanged(params string[] properties)
        {
            properties.ForEach(RaisePropertyChanged);
        }

        public void RaisingPropertyChanging(string propertyName)
        {
        }
    }
}