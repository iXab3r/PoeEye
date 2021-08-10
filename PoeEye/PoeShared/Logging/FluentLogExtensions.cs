using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DynamicData;
using PoeShared.Scaffolding;

namespace PoeShared.Logging
{
    public static class FluentLogExtensions
    {
        public static IFluentLog CreateChildCollectionLogWriter(this IFluentLog log, ISourceList<string> collection)
        {
            var writerAdapter = new LogWriterAdapter<string>(logData =>
            {
                log.Writer.WriteLog(logData);
                var message = logData.ToString();
                if (logData.Exception != null)
                {
                    message += Environment.NewLine + logData.Exception.Message + Environment.NewLine + logData.Exception.StackTrace;
                }
                collection.Add(message);
            });
            return new FluentLogBuilder(writerAdapter);
        }
        
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
        
        public static IFluentLog WithSuffix(this IFluentLog log, Func<string> suffixSupplier)
        {
            var newLogData = log.Data.WithSuffix(() =>
            {
                var suffix = suffixSupplier() ;
                return string.IsNullOrEmpty(suffix) ? null : $" [{suffix}]";
            });
            return log.WithLogData(newLogData);
        }
        
        public static IFluentLog WithSuffix<T>(this IFluentLog log, T prefix)
        {
            return WithSuffix(log, () => $"{prefix}");
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