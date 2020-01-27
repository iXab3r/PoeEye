using System;
using System.IO;
using System.Reflection;
using log4net;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding;


namespace PoeShared.Native
{
    public static class ResourceReader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ResourceReader));

        public static string ReadResourceAsString(string path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            var assembly = Assembly.GetExecutingAssembly();
            return ReadResourceAsString(assembly, path);
        }

        public static string ReadResourceAsString(Assembly assembly, string path)
        {
            Guard.ArgumentNotNull(assembly, nameof(assembly));
            Guard.ArgumentNotNull(path, nameof(path));

            var assemblyName = assembly.GetName().Name;
            var resourcePath = $"{assemblyName}.{path}";

            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    var resources = assembly.GetManifestResourceNames();
                    throw new FileNotFoundException($"Could not find resource '{resourcePath}' among the following:\r\n\t{string.Join("\r\n\t", resources)}");
                }

                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
        
        public static bool TryToLoadResourceByName(Assembly assembly, string name, out byte[] resourceData)
        {
            Guard.ArgumentNotNull(assembly, nameof(assembly));
            Guard.ArgumentNotNull(name, nameof(name));
            var internalResourceName = name;
            if (string.IsNullOrEmpty(internalResourceName))
            {
                Log.Debug($"Failed to find internal resource name for '{name}'");

                resourceData = null;
                return false;
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