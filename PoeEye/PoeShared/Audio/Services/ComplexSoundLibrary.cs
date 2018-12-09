using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;

namespace PoeShared.Audio.Services
{
    internal class ComplexSoundLibrary : ISoundLibrarySource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComplexSoundLibrary));

        private static readonly Lazy<ComplexSoundLibrary> InstanceSupplier = new Lazy<ComplexSoundLibrary>();
        private readonly ISoundLibrarySource[] sources;

        public ComplexSoundLibrary(ISoundLibrarySource[] sources)
        {
            this.sources = sources;
        }

        public static ComplexSoundLibrary Instance => InstanceSupplier.Value;

        public bool TryToLoadSourceByName(string name, out byte[] waveBytes)
        {
            foreach (var soundLibrarySource in sources)
            {
                if (soundLibrarySource.TryToLoadSourceByName(name, out waveBytes))
                {
                    return true;
                }
            }

            waveBytes = null;
            return false;
        }

        public IEnumerable<string> SourceName => sources.SelectMany(x => x.SourceName);
    }
}