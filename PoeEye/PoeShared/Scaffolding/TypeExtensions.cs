using System.Reflection;
using log4net;

namespace PoeShared.Scaffolding;

public static class TypeExtensions
{
    public static IFluentLog PrepareLogger(this Type type, string name = default)
    {
        return LogManager.GetLogger(type.GetTypeInfo().Assembly, string.IsNullOrEmpty(name) 
            ? type.ToString() 
            : name).ToFluent();
    }
        
    public static object GetDefault(this Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public static MethodInfo GetMethodOrThrow(this Type type, string methodName)
    {
        return type.GetMethod(methodName)
               ?? throw new MissingMethodException($"Failed to find method {methodName} in {type}");
    }
    
    public static MethodInfo GetMethodOrThrow(this Type type, string methodName, BindingFlags bindingFlags)
    {
        return type.GetMethod(methodName, bindingFlags)
               ?? throw new MissingMethodException($"Failed to find method {methodName} in {type} using binding flags {bindingFlags}");
    }
    
    public static ConstructorInfo GetConstructorOrThrow(this Type type, params Type[] types)
    {
        return type.GetConstructor(types)
               ?? throw new MissingMethodException($"Failed to find constructor in {type} for types: {types.Select(x => x.Name).DumpToString()}");
    }
    
    /// <summary>
    /// Gets all base types and interfaces for the specified type, including those inherited by its base types.
    /// </summary>
    /// <param name="type">The type for which to retrieve base types and interfaces.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the specified type, all of its base types, and all of its interfaces.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the specified type is null.</exception>
    public static IEnumerable<Type> GetAllBaseTypesAndInterfaces(this Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type), "Type cannot be null.");
        }

        var types = new HashSet<Type>();
        CollectTypes(type, types);
        return types;
    }

    private static void CollectTypes(Type type, HashSet<Type> types)
    {
        if (type == null || !types.Add(type))
        {
            // Base case for recursion: type is null or already processed
            return;
        }

        // Add interfaces implemented by this type
        foreach (var interfaceType in type.GetInterfaces())
        {
            CollectTypes(interfaceType, types);
        }

        // Recursive call for the base type
        CollectTypes(type.BaseType, types);
    }
}