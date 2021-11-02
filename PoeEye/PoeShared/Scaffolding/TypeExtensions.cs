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
    }
}