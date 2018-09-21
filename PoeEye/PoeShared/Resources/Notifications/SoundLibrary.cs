﻿using System.Reflection;
using Common.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Resources.Notifications
{
    internal static class SoundLibrary
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SoundLibrary));

        static SoundLibrary()
        {
            Log.Debug(
                $"[SoundLibrary..staticctor] Embedded resources list:\r\n {EnumerateResources().DumpToText()}");
        }

        public static bool TryToLoadSoundByName(string name, out byte[] waveBytes)
        {
            return TryToLoadResourceByName($"{name}.wav", out waveBytes);
        }

        private static bool TryToLoadResourceByName(string name, out byte[] resourceData)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var namespaceName = typeof(SoundLibrary).Namespace;
            var resourceName = $"{namespaceName}.{name}";
            Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Loading resource '{resourceName}'");

            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                Log.Debug($"[SoundLibrary.TryToLoadResourceByName] Resource was not found '{resourceName}'");
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

        private static string[] EnumerateResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
        }
    }
}