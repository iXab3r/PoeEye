namespace PoeShared.Utilities
{
    using Newtonsoft.Json;

    public static class ObjectExtensions
    {
        public static string DumpToText<T>(this T instance)
        {
            return instance == null ? $"null<{typeof (T).Name}>" : JsonConvert.SerializeObject(instance);
        } 
    }
}