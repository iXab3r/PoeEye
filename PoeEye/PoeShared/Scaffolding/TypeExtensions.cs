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
}