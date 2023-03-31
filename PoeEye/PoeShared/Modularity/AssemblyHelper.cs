using System.Reflection;

namespace PoeShared.Modularity;

public sealed class AssemblyHelper
{
    private static readonly IFluentLog Log = typeof(AssemblyHelper).PrepareLogger();

    private readonly ConcurrentDictionary<string, Assembly> loadedAssemblyByName = new();
    private readonly ConcurrentDictionary<string, Type> loadedTypesByName = new();

    private static readonly Lazy<AssemblyHelper> AssemblyHelperSupplier = new();

    public static AssemblyHelper Instance => AssemblyHelperSupplier.Value;

    public bool AttemptToLoadIfNotResolved { get; set; } = false;

    public Type ResolveType(PoeConfigMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.TypeName))
        {
            Log.Warn($"Invalid metadata: {metadata}");
            throw new ArgumentException($"Invalid metadata - typename is not set");
        }

        {
            if (loadedTypesByName.TryGetValue(metadata.TypeName, out var loadedType))
            {
                return loadedType;
            }
        }

        {
            var loadedType = Type.GetType(metadata.TypeName, false);
            if (loadedType != null)
            {
                loadedTypesByName[metadata.TypeName] = loadedType;
                return loadedType;
            }
        }

        // at this point we really need assembly name
        if (string.IsNullOrEmpty(metadata.AssemblyName))
        {
            throw new FormatException($"Metadata is not valid(assembly is not defined): {metadata}");
        }

        var typeNameSplit = metadata.TypeName.SplitTrim(".");
        var candidates = new HashSet<string>()
        {
            metadata.AssemblyName,
            typeNameSplit[0]
        };
        if (typeNameSplit.Length > 1)
        {
            candidates.Add(typeNameSplit.Take(2).JoinStrings("."));
        }

        candidates.ToArray().ForEach(x => candidates.Add($"{x}.Metadata"));

        foreach (var candidateAssembly in candidates)
        {
            Log.Debug(() => $"Type {metadata.TypeName} is not loaded, trying to read type from assembly {metadata.AssemblyName}");

            if (TryToLoadFromAssemblyName(candidateAssembly, metadata.TypeName, out var loadedType))
            {
                loadedTypesByName[metadata.TypeName] = loadedType;
                return loadedType;
            }
        }

        Log.Debug($"Failed to resolve type {metadata.TypeName}, (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())}), probed assemblies: {candidates.JoinStrings(", ")}");
        return null;
    }

    private Assembly ResolveAssembly(string assemblyName)
    {
        if (loadedAssemblyByName.TryGetValue(assemblyName, out var assembly))
        {
            return assembly;
        }

        assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == assemblyName);
        if (assembly != null)
        {
            loadedAssemblyByName[assemblyName] = assembly;
            return assembly;
        }

        if (AttemptToLoadIfNotResolved)
        {
            Log.Debug($"Assembly {assemblyName} is not resolved, attempting to load it");
            try
            {
                assembly = Assembly.Load(new AssemblyName(assemblyName));
            }
            catch (FileNotFoundException)
            {
                Log.Debug($"Failed to load assembly {assemblyName} - not found");
            }
            catch (BadImageFormatException)
            {
                Log.Debug($"Failed to load assembly {assemblyName} - incompatible image format");
            }
            catch (FileLoadException)
            {
                Log.Debug($"Failed to load assembly {assemblyName} - something is wrong with the file");
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to load assembly {assemblyName}", e);
            }

            loadedAssemblyByName[assemblyName] = assembly;
            if (assembly != null)
            {
                Log.Debug($"Successfully loaded {assemblyName}: {assembly}");
                return assembly;
            }
        }

        Log.Debug($"Assembly {assemblyName} is not resolved");
        return null;
    }

    private bool TryToLoadFromAssemblyName(string assemblyName, string typeName, out Type type)
    {
        var assembly = ResolveAssembly(assemblyName);
        if (assembly == null)
        {
            Log.Debug($"Assembly {assemblyName} is not loaded/not found, could not resolve type {typeName}");
            type = null;
            return false;
        }

        var loadedType = assembly.GetType(typeName, throwOnError: false);
        if (loadedType != null)
        {
            type = loadedType;
            return true;
        }

        Log.Debug($"Assembly {assemblyName} is loaded but does not contain type {typeName}");
        type = null;
        return false;
    }
}