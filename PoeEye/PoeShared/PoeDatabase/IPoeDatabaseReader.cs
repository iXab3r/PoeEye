using System;
using System.Collections.ObjectModel;

namespace PoeShared.PoeDatabase
{
    using JetBrains.Annotations;

    public interface IPoeDatabaseReader : IDisposable
    {
        ReadOnlyObservableCollection<string> KnownEntityNames { [NotNull] get; }
    }
}