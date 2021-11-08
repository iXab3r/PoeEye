using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;
using PoeShared.Logging;

namespace PoeShared.Scaffolding
{
    public static class TypeExtensions
    {
        public static IFluentLog PrepareLogger(this Type type, string name = default)
        {
            return LogManager.GetLogger(type.GetTypeInfo().Assembly, string.IsNullOrEmpty(name) 
                ? type.ToString() 
                : name).ToFluent();
        }
        
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();
        
            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }
        
            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;
        
            var baseType = givenType.BaseType;
            if (baseType == null)
            {
                return false;
            }
        
            return IsAssignableToGenericType(baseType, genericType);
        }
    }
}