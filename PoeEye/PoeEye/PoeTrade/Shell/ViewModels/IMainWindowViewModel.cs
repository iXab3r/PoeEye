using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeEye.PoeTrade.ViewModels;

namespace PoeEye.PoeTrade.Shell.ViewModels
{
    internal interface IMainWindowViewModel : IDisposable
    {
        ReadOnlyObservableCollection<IMainWindowTabViewModel> TabsList { [NotNull] get; }
    }
}