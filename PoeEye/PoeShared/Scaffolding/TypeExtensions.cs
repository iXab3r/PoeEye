using System;
using System.Reflection;
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
    }
}