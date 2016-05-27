using System.Collections.Generic;

namespace PoePickitTestApp.Extensions
{
    internal static class ObjectExtensions
    {
        public static void AddTo<T>(this T item, ICollection<T> collection)
        {
            collection.Add(item);
        }
    }
}