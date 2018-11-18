using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services
{
    internal class ComplexSoundLibrary : ISoundLibrarySource
    {
        private readonly ISoundLibrarySource[] sources;
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComplexSoundLibrary));

        private static readonly Lazy<ComplexSoundLibrary> InstanceSupplier = new Lazy<ComplexSoundLibrary>();
        public static ComplexSoundLibrary Instance => InstanceSupplier.Value;
        
        public ComplexSoundLibrary(ISoundLibrarySource[] sources)
        {
            this.sources = sources;
        }

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