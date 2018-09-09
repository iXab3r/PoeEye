using System.IO;
using System.Reflection;
using Guards;

namespace PoeShared.Scaffolding
{
    public static class ResourceReader
    {
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
    }
}
