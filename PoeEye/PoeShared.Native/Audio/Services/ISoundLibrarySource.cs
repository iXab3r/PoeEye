using System.Collections.Generic;
using JetBrains.Annotations;

namespace PoeShared.Audio.Services
{
    internal interface ISoundLibrarySource
    {
        IEnumerable<string> SourceName { [NotNull] get; }

        bool TryToLoadSourceByName([NotNull] string name, out byte[] waveData);
    }
}