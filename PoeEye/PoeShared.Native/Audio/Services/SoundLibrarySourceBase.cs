using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PoeShared.Audio.Services
{
    internal abstract class SoundLibrarySourceBase : ISoundLibrarySource
    {
        public abstract IEnumerable<string> SourceName { get; }

        public abstract bool TryToLoadSourceByName(string name, out byte[] waveData);

        protected ISet<string> GetSupportedExtensions()
        {
            return new HashSet<string>(new[] {".wav", ".mp3"});
        }

        protected ISet<string> FormatFileName(string fileName)
        {
            var result = GetSupportedExtensions()
                .Select(x => Path.ChangeExtension(fileName, x));
            return new HashSet<string>(result);
        }
    }
}