namespace PoeShared.Scaffolding
{
    using System.Collections.Generic;

    using Guards;

    using Newtonsoft.Json;

    public static class ObjectExtensions
    {
        public static string DumpToText<T>(this T instance)
        {
            return instance == null ? $"null<{typeof(T).Name}>" : JsonConvert.SerializeObject(instance, Formatting.Indented);
        }

        public static T AddTo<T>(this T instance, ICollection<T> collection)
        {
            Guard.ArgumentNotNull(() => instance);
            Guard.ArgumentNotNull(() => collection);
            
            collection.Add(instance);
            return instance;
        }
    }
}