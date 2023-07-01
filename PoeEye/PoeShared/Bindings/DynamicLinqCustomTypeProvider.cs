using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PoeShared.Bindings;

/// <summary>
///     The default implementation for <see cref="IDynamicLinkCustomTypeProvider" />.
///     Scans the current AppDomain for all types marked with <see cref="DynamicLinqTypeAttribute" />, and adds them as
///     custom Dynamic Link types.
///     Also provides functionality to resolve a Type in the current Application Domain.
///     This class is used as default for full .NET Framework, so not for .NET Core
/// </summary>
[Obsolete("This class is an absolute garbage, leftover from DynamicLinq lib")]
internal sealed class DynamicLinqCustomTypeProvider : AbstractDynamicLinqCustomTypeProvider, IDynamicLinkCustomTypeProvider
{
    private static readonly IFluentLog Log = typeof(DynamicLinqCustomTypeProvider).PrepareLogger();

    private readonly IAssemblyHelper assemblyHelper = new DefaultAssemblyHelper();

    private static readonly ConcurrentDictionary<AssemblyName, Type[]> TypesByAssembly = new();

    private HashSet<Type> cachedCustomTypes;
    private Dictionary<Type, List<MethodInfo>> cachedExtensionMethods;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultDynamicLinqCustomTypeProvider" /> class.
    /// </summary>
    public DynamicLinqCustomTypeProvider()
    {
    }

    /// <inheritdoc cref="IDynamicLinqCustomTypeProvider.GetCustomTypes" />
    public HashSet<Type> GetCustomTypes()
    {
        return cachedCustomTypes ??= GetCustomTypesInternal();
    }

    /// <inheritdoc cref="IDynamicLinqCustomTypeProvider.GetExtensionMethods" />
    public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
    {
        return cachedExtensionMethods ??= GetExtensionMethodsInternal();
    }

    /// <inheritdoc cref="IDynamicLinqCustomTypeProvider.ResolveType" />
    public Type ResolveType(string typeName)
    {
        IEnumerable<Assembly> assemblies = assemblyHelper.GetAssemblies();
        return ResolveType(assemblies, typeName);
    }

    /// <inheritdoc cref="IDynamicLinqCustomTypeProvider.ResolveTypeBySimpleName" />
    public Type ResolveTypeBySimpleName(string simpleTypeName)
    {
        IEnumerable<Assembly> assemblies = assemblyHelper.GetAssemblies();
        return ResolveTypeBySimpleName(assemblies, simpleTypeName);
    }

    private HashSet<Type> GetCustomTypesInternal()
    {
        IEnumerable<Assembly> assemblies = assemblyHelper.GetAssemblies();
        return new HashSet<Type>(FindTypesMarkedWithDynamicLinqTypeAttribute(assemblies));
    }

    private Dictionary<Type, List<MethodInfo>> GetExtensionMethodsInternal()
    {
        var types = GetCustomTypes();

        var list = new List<Tuple<Type, MethodInfo>>();

        foreach (var type in types)
        {
            var extensionMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.IsDefined(typeof(ExtensionAttribute), false)).ToList();

            extensionMethods.ForEach(x => list.Add(new Tuple<Type, MethodInfo>(x.GetParameters()[0].ParameterType, x)));
        }

        return list.GroupBy(x => x.Item1, tuple => tuple.Item2).ToDictionary(key => key.Key, methods => methods.ToList());
    }


    /// <summary>
    ///     Resolve any type which is registered in the current application domain.
    /// </summary>
    /// <param name="assemblies">The assemblies to inspect.</param>
    /// <param name="typeName">The typename to resolve.</param>
    /// <returns>A resolved <see cref="Type" /> or null when not found.</returns>
    private new static Type ResolveType(IEnumerable<Assembly> assemblies, string typeName)
    {
        foreach (var assembly in assemblies)
        {
            var resolvedType = assembly.GetType(typeName, false, true);
            if (resolvedType != null)
            {
                return resolvedType;
            }
        }

        return null;
    }

    /// <summary>
    ///     Resolve a type by the simple name which is registered in the current application domain.
    /// </summary>
    /// <param name="assemblies">The assemblies to inspect.</param>
    /// <param name="simpleTypeName">The simple typename to resolve.</param>
    /// <returns>A resolved <see cref="Type" /> or null when not found.</returns>
    private new static Type ResolveTypeBySimpleName(IEnumerable<Assembly> assemblies, string simpleTypeName)
    {
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = TypesByAssembly.GetOrAdd(assembly.GetName(), name => GetTypesSafe(assembly));
                var fullnames = types.Select(t => t.FullName).Distinct();
                var firstMatchingFullname = fullnames.FirstOrDefault(fn => fn.EndsWith($".{simpleTypeName}"));

                if (firstMatchingFullname != null)
                {
                    var resolvedType = assembly.GetType(firstMatchingFullname, false, true);
                    if (resolvedType != null)
                    {
                        return resolvedType;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to process assembly {assembly}", e);
            }
        }

        return null;
    }

    private class DefaultAssemblyHelper : IAssemblyHelper
    {
        public Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
    }

    private static Type[] GetTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (Exception e)
        {
            return Type.EmptyTypes;
        }
    }
}