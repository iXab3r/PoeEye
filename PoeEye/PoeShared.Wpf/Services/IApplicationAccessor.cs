using System;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;

namespace PoeShared.Services
{
    public interface IApplicationAccessor : INotifyPropertyChanged
    {
        IObservable<Unit> WhenExit { get; }

        Task Exit();
        
        bool IsExiting { get; }
    }
}