using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Resources.Notifications
{
    internal static class SoundLibrary
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SoundLibrary));

        private static string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        static SoundLibrary()
        {
            Log.Debug(
                $"[SoundLibrary..staticctor] Embedded resources list:\r\n {EnumerateResources().DumpToText()}");
        }

        public static bool TryToLoadSoundByName(string name, out byte[] waveBytes)
        {
            return TryToLoadByName(name, out waveBytes) || TryToLoadResourceByName(name, out waveBytes);
        }
        
        private static bool TryToLoadResourceByName(string name, out byte[] resourceData)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var namespaceName = typeof(SoundLibrary).Namespace;
            var resourceName = $"{namespaceName}.{name}.wav";
            Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Mapping resource '{resourceName}' to real resource name...");

            var internalResourceName = resources.FirstOrDefault(x => string.Equals(x, resourceName, StringComparison.OrdinalIgnoreCase));
            if (internalResourceName != resourceName)
            {
                Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Real resource name: '{internalResourceName}', initial: {resourceName}...");
                resourceName = internalResourceName;
            }
            
            Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Loading resource '{resourceName}'...");
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                var resourcesList = assembly.GetManifestResourceNames();
                Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Resource was not found '{resourceName}', embedded res.list: {resourcesList.DumpToTextRaw()}");
                resourceData = null;
                return false;
            }

            using (var stream = resourceStream)
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Loaded resource '{resourceName}' : {buffer.Length}b");
                resourceData = buffer;
                return true;
            }
        }

        private static bool TryToLoadByName(string name, out byte[] resourceData)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Notifications", $"{name}.wav");
            Log.Debug($"[SoundLibrary.TryToLoadByName] Loading file '{filePath}'");

            if (!File.Exists(filePath))
            {
                Log.Debug($"[SoundLibrary.TryToLoadByName] File was not found '{filePath}'");
                resourceData = null;
                return false;
            }

            using (var stream = File.OpenRead(filePath))
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Loaded file '{filePath}' : {buffer.Length}b");
                resourceData = buffer;
                return true;
            }
        }

        private static string[] EnumerateResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
        }
    }
}