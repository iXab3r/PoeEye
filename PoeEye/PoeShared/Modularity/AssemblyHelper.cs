using System.Collections.Immutable;
using System.Reflection;

namespace PoeShared.Modularity;

/// <summary>
/// Provides helper methods for resolving and loading assemblies and types dynamically.
/// Includes caching, configurable auto-loading, and per-thread resolution context.
/// </summary>
public sealed class AssemblyHelper
{
    public static AssemblyHelper Instance => AssemblyHelperSupplier.Value;

    private static readonly IFluentLog Log = typeof(AssemblyHelper).PrepareLogger();
    private static readonly Lazy<AssemblyHelper> AssemblyHelperSupplier = new();

    /// <summary>
    /// This is an array of additional assemblies that will be analyzed first during resolution
    /// Use case - when we have some generated-on-the-fly assemblies which must take preference over those loaded earlier
    /// </summary>
    [ThreadStatic] private static RefCountedLinkedSet<Assembly> resolutionContextSource;
    private static RefCountedLinkedSet<Assembly> ResolutionContext => resolutionContextSource ??= new RefCountedLinkedSet<Assembly>();

    private readonly ConcurrentDictionary<string, Assembly> loadedAssemblyByName = new();
    private readonly ConcurrentDictionary<string, Type> loadedTypesByName = new();

    /// <summary>
    /// Gets or sets a value indicating whether the helper should attempt to load assemblies automatically
    /// when they cannot be resolved from the current AppDomain.
    /// </summary>
    public bool AttemptToLoadIfNotResolved { get; set; } = false;

    /// <summary>
    /// Pushes a set of assemblies into the thread-local resolution context, which will be checked first
    /// when resolving types.
    /// </summary>
    /// <param name="assembly">The assemblies to push into the resolution context.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that, when disposed, restores the previous resolution context.
    /// Must be disposed on the same thread that called this method.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the disposable is invoked from a different thread than the one where <see cref="PushAssemblies"/> was called.
    /// </exception>    
    public IDisposable PushAssemblies(Assembly assembly)
    {
        var pushThread = Environment.CurrentManagedThreadId;
        var anchor = ResolutionContext.Add(assembly);
        
        return Disposable.Create(() =>
        {
            if (pushThread != Environment.CurrentManagedThreadId)
            {
                throw new InvalidOperationException($"Disposal must happen on the same thread where Push was, current thread: {Environment.CurrentManagedThreadId}, expected: {pushThread}");
            }

            anchor.Dispose();
        });
    }

    /// <summary>
    /// Resolves a <see cref="Type"/> from the provided metadata, searching the resolution context,
    /// cached types, loaded assemblies, and optionally attempting to load missing assemblies.
    /// </summary>
    /// <param name="metadata">The metadata describing the type to resolve.</param>
    /// <returns>
    /// The resolved <see cref="Type"/> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="metadata"/> does not specify a valid type name.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if assembly name is missing from the metadata and the type is not already resolvable.
    /// </exception>
    public Type ResolveType(PoeConfigMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.TypeName))
        {
            Log.Warn($"Invalid metadata: {metadata}");
            throw new ArgumentException($"Invalid metadata - typename is not set");
        }

        foreach (var candidateAssembly in ResolutionContext)
        {
            if (TryToLoadFromAssembly(candidateAssembly, metadata.TypeName, out var loadedType))
            {
                //do not cache this
                return loadedType;
            }
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
            Log.Debug($"Type {metadata.TypeName} is not loaded, trying to read type from assembly {metadata.AssemblyName}");

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

        return TryToLoadFromAssembly(assembly, typeName, out type);
    }

    private static bool TryToLoadFromAssembly(Assembly assembly, string typeName, out Type type)
    {
        var loadedType = assembly.GetType(typeName, throwOnError: false);
        if (loadedType != null)
        {
            type = loadedType;
            return true;
        }

        Log.Debug($"Assembly {assembly.FullName} is loaded but does not contain type {typeName}");
        type = null;
        return false;
    }
}