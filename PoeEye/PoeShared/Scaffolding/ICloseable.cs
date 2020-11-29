using System;
using System.ComponentModel;

namespace PoeShared.Scaffolding
{
    public interface ICloseable : INotifyPropertyChanged
    {
        public ICloseController CloseController { get; set; }
    }
}