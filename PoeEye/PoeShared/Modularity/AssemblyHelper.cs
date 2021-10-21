using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    public sealed class AssemblyHelper
    {
        private static readonly IFluentLog Log = typeof(AssemblyHelper).PrepareLogger();

        private readonly ConcurrentDictionary<string, Assembly> loadedAssemblyByName = new ConcurrentDictionary<string, Assembly>();

        private static readonly Lazy<AssemblyHelper> AssemblyHelperSupplier = new Lazy<AssemblyHelper>();

        public static AssemblyHelper Instance => AssemblyHelperSupplier.Value;
        
        public AssemblyHelper()
        {
        }

        public Type ResolveType(PoeConfigMetadata metadata)
        {
            var loadedType = Type.GetType(metadata.TypeName, false);
            if (loadedType != null)
            {
                return loadedType;
            }
            
            if (string.IsNullOrEmpty(metadata.AssemblyName))
            {
                throw new FormatException($"Metadata is not valid(assembly is not defined): {metadata}");
            }
            
            Log.Debug($"Type {metadata.TypeName} is not loaded, trying to read type from assembly {metadata.AssemblyName}");
            if (!loadedAssemblyByName.TryGetValue(metadata.AssemblyName, out var assembly))
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == metadata.AssemblyName);
                if (assembly == null)
                {
                    Log.Warn($"Assembly {metadata.AssemblyName} is not loaded, could not resolve type {metadata.TypeName} (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())})");
                    return null;
                }

                loadedAssemblyByName[metadata.AssemblyName] = assembly;
            }
            
            loadedType = assembly.GetType(metadata.TypeName, throwOnError: false);
            if (loadedType == null)
            {
                Log.Warn($"Assembly {metadata.AssemblyName} is loaded but does not contain type {metadata.TypeName}, (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())})");
            }
            return loadedType;
        }
    }
}