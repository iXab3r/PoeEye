using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace PoeShared.PoeDatabase
{
    public interface IPoeDatabaseReader : IDisposable
    {
        ReadOnlyObservableCollection<string> KnownEntityNames { [NotNull] get; }
    }
}