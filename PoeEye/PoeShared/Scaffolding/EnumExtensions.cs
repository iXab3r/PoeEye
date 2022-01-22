using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeShared.Scaffolding;

public static class EnumExtensions
{
    public static T RemoveFlag<T>(this T flag, params T[] flagsToRemove) where T : Enum
    {
        return flagsToRemove.Aggregate(flag, RemoveFlag);
    }

    public static T RemoveFlag<T>(this T flag, T flagToRemove) where T : Enum
    {
        try
        {
            if (!flag.HasFlag(flagToRemove))
            {
                return flag;
            }

            var maskValue = ~ Convert.ToInt64(flagToRemove);
            var flagValue = Convert.ToInt64(flag);

            return (T)Enum.ToObject(typeof(T), flagValue & maskValue);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Could not remove flag value {flagToRemove} from {flag}, enum {typeof(T).Name}", ex);
        }
    }

    public static IEnumerable<T> GetUniqueFlags<T>(this T flags)
        where T : Enum 
    {
        return from Enum value in Enum.GetValues(flags.GetType()) where flags.HasFlag(value) select (T)value;
    }
}