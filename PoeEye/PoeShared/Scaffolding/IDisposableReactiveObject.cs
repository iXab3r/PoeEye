using System.ComponentModel;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public interface IDisposableReactiveObject : IDisposable, INotifyPropertyChanged
{
    CompositeDisposable Anchors { [NotNull] get; }

    void RaisePropertyChanged([CanBeNull] string propertyName);
}