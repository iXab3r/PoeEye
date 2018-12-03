using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding;

namespace PoeShared.Resources.Notifications
{
    internal class EmbeddedSoundLibrarySource : SoundLibrarySourceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EmbeddedSoundLibrarySource));

        private static readonly string[] EmbeddedResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        public EmbeddedSoundLibrarySource()
        {
            Log.Debug($"Embedded resources list:\r\n {EmbeddedResourceNames.DumpToText()}");

            var namespaceName = typeof(EmbeddedSoundLibrarySource).Namespace;
            var extensions = GetSupportedExtensions();
            var sources = EmbeddedResourceNames
                .Select(x => new {InternalResourceName = x, SourceName = Path.GetFileNameWithoutExtension(x.Replace($"{namespaceName}.", string.Empty))})
                .Where(x => extensions.Any(ext => string.Equals(ext, Path.GetExtension(x.InternalResourceName), StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.SourceName)
                .ToArray();
            SourceName = sources;

            Log.Debug($"Source name list(namespace: {namespaceName}):\r\n {sources.DumpToText()}");
        }

        public override IEnumerable<string> SourceName { get; }

        public override bool TryToLoadSourceByName(string name, out byte[] resourceData)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var namespaceName = typeof(EmbeddedSoundLibrarySource).Namespace;
            var resourceNames = FormatFileName(name)
                .Select(x => $"{namespaceName}.{x}")
                .ToArray();
            Log.Debug($"Mapping resources '{resourceNames.DumpToTextRaw()}' to real resource name...");

            string internalResourceName = null;
            foreach (var realResourceName in EmbeddedResourceNames)
            {
                foreach (var resourceName in resourceNames)
                {
                    if (!string.Equals(resourceName, realResourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    Log.Debug($"Real resource name: '{realResourceName}', initial: {resourceName}...");
                    internalResourceName = realResourceName;
                    break;
                }
            }

            Log.Debug($"Loading resource '{internalResourceName}'...");
            var resourceStream = assembly.GetManifestResourceStream(internalResourceName);
            if (resourceStream == null)
            {
                var resourcesList = assembly.GetManifestResourceNames();
                Log.Debug($"Resource was not found '{internalResourceName}', embedded res.list: {resourcesList.DumpToTextRaw()}");
                resourceData = null;
                return false;
            }

            using (var stream = resourceStream)
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Log.Debug($"Loaded resource '{internalResourceName}' : {buffer.Length}b");
                resourceData = buffer;
                return true;
            }
        }
    }
}