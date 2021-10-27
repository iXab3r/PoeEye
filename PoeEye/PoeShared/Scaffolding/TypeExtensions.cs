using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;
using PoeShared.Logging;

namespace PoeShared.Scaffolding
{
    public static class TypeExtensions
    {
        public static IFluentLog PrepareLogger(this Type type)
        {
            return LogManager.GetLogger(type.GetTypeInfo().Assembly, type.ToString()).ToFluent();
        }
        
        public static IFluentLog PrepareLogger(this object instance, [CallerMemberName] string caller = default)
        {
            if (string.IsNullOrEmpty(caller))
            {
                throw new ArgumentException($"Caller must be specified, but was not, instance: {instance}");
            }
            return LogManager.GetLogger(Assembly.GetExecutingAssembly(), caller).ToFluent();
        }
    }
}