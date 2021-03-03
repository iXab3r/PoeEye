using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services
{
    internal class ComplexSoundLibrary : DisposableReactiveObject, ISoundLibrarySource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComplexSoundLibrary));

        private static readonly Lazy<ComplexSoundLibrary> InstanceSupplier = new Lazy<ComplexSoundLibrary>();
        private readonly ISoundLibrarySource[] sources;

        public ComplexSoundLibrary(ISoundLibrarySource[] sources)
        {
            this.sources = sources;
            
            new SourceList<string>().Connect()
                .Or(sources.Select(x => x.SourceName.ToObservableChangeSet()).ToArray())
                .Transform(x => x.ToLowerInvariant())
                .AddKey(x => x)
                .Bind(out var sourceNames)
                .Subscribe()
                .AddTo(Anchors);
            SourceName = sourceNames;
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

        public ReadOnlyObservableCollection<string> SourceName { get; }
    }
}