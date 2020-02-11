using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding;

namespace PoeShared.Native.Resources.Notifications
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
            var resourceNameCandidates = FormatFileName(name)
                .Select(x => $"{namespaceName}.{x}")
                .ToArray();
            Log.Debug($"Trying to find resource using names '{resourceNameCandidates.DumpToTextRaw()}'...");

            string resourceName = null;
            foreach (var embeddedResourceName in EmbeddedResourceNames)
            {
                foreach (var resourceNameCandidate in resourceNameCandidates)
                {
                    if (!string.Equals(resourceNameCandidate, embeddedResourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (embeddedResourceName != resourceNameCandidate)
                    {
                        Log.Debug($"Embedded resource name: '{embeddedResourceName}', candidate: {resourceNameCandidate}...");
                    }
                    resourceName = embeddedResourceName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(resourceName))
            {
                Log.Debug($"Failed to find internal resource name for '{name}'");

                resourceData = null;
                return false;
            }

            Log.Debug($"Loading resource '{resourceName}'...");
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                var resourcesList = assembly.GetManifestResourceNames();
                Log.Debug($"Resource was not found '{resourceName}', embedded res.list: {resourcesList.DumpToTextRaw()}");
                resourceData = null;
                return false;
            }

            using (var stream = resourceStream)
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Log.Debug($"Loaded resource '{resourceName}' : {buffer.Length}b");
                resourceData = buffer;
                return true;
            }
        }
    }
}