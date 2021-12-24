using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

namespace PoeShared.Scaffolding
{
    public abstract class DisposableReactiveObject : IDisposableReactiveObject
    {
        public CompositeDisposable Anchors { get; } = new CompositeDisposable();

        public virtual void Dispose()
        {
            Anchors.Dispose();
            GC.SuppressFinalize(this);
        }

        protected TRet RaiseAndSetIfChanged<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return newValue;
            }

            return RaiseAndSet(ref backingField, newValue, propertyName);
        }

        protected TRet RaiseAndSet<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            backingField = newValue;
            RaisePropertyChanged(propertyName);
            return newValue;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void AddDisposableResource<T>(Func<T> accessor) where T : IDisposable
        {
            Disposable.Create(() =>
            {
                var item = accessor();
                item?.Dispose();
            }).AddTo(Anchors);
        }

        protected void EnsureNonUiThread()
        {
            if (Environment.CurrentManagedThreadId == 1)
            {
                throw new InvalidOperationException($"Operation must be completed on non-UI thread");
            }
        }
        
        public void RaisePropertyChanged(params string[] properties)
        {
            properties.ForEach(RaisePropertyChanged);
        }
    }
}