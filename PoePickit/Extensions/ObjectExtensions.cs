namespace PoePickit.Extensions
{
    using Newtonsoft.Json;

    internal static class ObjectExtensions
    {
        public static string DumpToString(this object instance)
        {
            return instance == null ? "null" : JsonConvert.SerializeObject(instance, Formatting.Indented);
        }

        public static void DumpToConsole(this object instance)
        {
            ConsoleExtensions.WriteLine(instance.DumpToString(), System.ConsoleColor.White);
        }
    }
}