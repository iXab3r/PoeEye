using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Converters;

public sealed class MultiValueToTupleConverter : IMultiValueConverter
{
    private static readonly IFluentLog Log = typeof(MultiValueToTupleConverter).PrepareLogger();
    private static readonly ConcurrentDictionary<string, MethodInfo> ValueTupleMethodByTypes = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
        {
            return default;
        }

        return PrepareTuple(values);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static object PrepareTuple(object[] values)
    {
        var typeArguments = values.Select((x, idx) =>
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(values), $"Item #{idx} is null, provided values: {values.DumpToString()}");
            }
            return x.GetType();
        }).ToArray();
        return ValueTupleMethodByTypes.GetOrAdd(typeArguments.Select(x => x.Name).JoinStrings("+"), key => GetMethod(typeArguments));
    }

    private static MethodInfo GetMethod(Type[] typeArguments)
    {
        var methods = typeof(Tuple).GetMethods();
        var methodBase = methods.FirstOrDefault(method => method.Name == nameof(ValueTuple.Create) && method.GetParameters().Length == typeArguments.Length);
        if (methodBase == null)
        {
            Log.Warn($"Failed to find method for type arguments: {typeArguments.Select(x => x.Name).JoinStrings(", ")} in {typeof(ValueTuple)}, methods: \n\t{methods.Select(x => $"{x.Name} (params: {x.GetParameters().Select(y => y.Name)})").DumpToTable()}");
            throw new ArgumentException($"Failed to find method for type arguments: {typeArguments.Select(x => x.Name).JoinStrings(", ")} in {typeof(ValueTuple)}");
        }
        return methodBase.MakeGenericMethod(typeArguments);
    }
}