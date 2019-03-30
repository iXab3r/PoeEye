using System;
using System.Reactive;
using JetBrains.Annotations;

namespace PoeShared.UI.Models
{
    public interface IViewController
    {
        IObservable<Unit> Loaded { [NotNull] get; }
    }
}