#pragma warning disable CS0067
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public abstract record ReactiveRecord : INotifyPropertyChanged
{
    private readonly INpcEventInvoker propertyChanged;

    protected ReactiveRecord()
    {
        propertyChanged = new ConcurrentNpcEventInvoker(this);
    }

    public event PropertyChangedEventHandler PropertyChanged
    {
        add => propertyChanged.Add(value);
        remove => propertyChanged.Remove(value);
    }

    public void RaisePropertyChanged(string propertyName)
    {
        propertyChanged.Raise(propertyName);
    }

    [UsedImplicitly]
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

    protected TRet RaiseAndSet<TRet>(
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        backingField = newValue;
        RaisePropertyChanged(propertyName);
        return newValue;
    }
}