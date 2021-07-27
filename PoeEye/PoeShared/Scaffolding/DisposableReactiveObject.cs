using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
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

        public TRet RaiseAndSetIfChanged<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return newValue;
            }

            return RaiseAndSet(ref backingField, newValue, propertyName);
        }
        
        public TRet RaiseAndSet<TRet>(ref TRet backingField,
            TRet newValue,
            [CallerMemberName] string propertyName = null)
        {
            RaisingPropertyChanging(propertyName);
            backingField = newValue;
            RaisePropertyChanged(propertyName);
            return newValue;
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