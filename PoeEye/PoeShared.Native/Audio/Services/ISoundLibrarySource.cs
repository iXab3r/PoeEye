using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace PoeShared.Audio.Services
{
    internal interface ISoundLibrarySource
    {
        ReadOnlyObservableCollection<string> SourceName { [NotNull] get; }

        bool TryToLoadSourceByName([NotNull] string name, out byte[] waveData);
    }
}