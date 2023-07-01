using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;

namespace PoeShared.Bindings;

internal sealed class CachingCustomTypeProvider : IDynamicLinkCustomTypeProvider
{
    private readonly IDynamicLinkCustomTypeProvider source;

    private readonly ConcurrentDictionary<string, Type> typeBySimpleName = new();
    private readonly ConcurrentDictionary<string, Type> typeByName = new();

    public CachingCustomTypeProvider(IDynamicLinkCustomTypeProvider source)
    {
        this.source = source;
    }

    public HashSet<Type> GetCustomTypes()
    {
        return source.GetCustomTypes();
    }

    public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
    {
        return source.GetExtensionMethods();
    }

    public Type ResolveType(string typeName)
    {
        if (typeByName.TryGetValue(typeName, out var type) && type != null)
        {
            return type;
        }
        return typeByName.AddOrUpdate(typeName, () => source.ResolveType(typeName), (_, existing) => existing ?? source.ResolveType(typeName));
    }

    public Type ResolveTypeBySimpleName(string simpleTypeName)
    {
        if (typeBySimpleName.TryGetValue(simpleTypeName, out var type) && type != null)
        {
            return type;
        }
        return typeBySimpleName.AddOrUpdate(simpleTypeName, () => source.ResolveTypeBySimpleName(simpleTypeName), (_, existing) => existing ?? source.ResolveTypeBySimpleName(simpleTypeName));
    }
}