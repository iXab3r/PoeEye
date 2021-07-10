using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using PoeShared.Scaffolding;

namespace PoeShared.Logging
{
    public static class FluentLogExtensions
    {
        public static BenchmarkTimer CreateProfiler(this IFluentLog log, string benchmarkName, [CallerMemberName] string propertyName = null)
        {
            return new BenchmarkTimer(benchmarkName, log, propertyName);
        }

        public static IFluentLog WithPrefix(this IFluentLog log, Func<string> prefixSupplier)
        {
            var newLogData = log.Data.WithPrefix(() =>
            {
                var prefix = prefixSupplier() ;
                return string.IsNullOrEmpty(prefix) ? null : $"[{prefix}] ";
            });
            return log.WithLogData(newLogData);
        }
        
        public static IFluentLog WithPrefix<T>(this IFluentLog log, T prefix)
        {
            return WithPrefix(log, () => $"{prefix}");
        }

        public static IFluentLog WithTable<T>(this IFluentLog log, IEnumerable<T> items, string separator = "\n\t")
        {
            var newLogData = log.Data.WithSuffix(() =>
            {
                var result = new StringBuilder();
                var count = 0;
                foreach (var item in items)
                {
                    result.Append(separator);
                    count++;
                    result.Append($"#{count} {item}");
                }

                return $"{separator}Items: {count}{result}";
            });
            return log.WithLogData(newLogData);
        }
    }
}