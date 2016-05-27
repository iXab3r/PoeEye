using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace PoePricer.Extensions
{
    internal static class ObjectExtensions
    {
        public static string DumpToString(this object instance)
        {
            return instance == null ? "null" : JsonConvert.SerializeObject(instance, Formatting.Indented);
        }

        public static void DumpToConsole(this object instance)
        {
            ConsoleExtensions.WriteLine(instance.DumpToString(), ConsoleColor.White);
        }
    }
}