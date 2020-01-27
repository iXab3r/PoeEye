using System;

namespace PoeShared.Scaffolding
{
    public static class ComparableExtensions
    {
        public static bool IsGreaterThan<T>(this T value, T other) where T : IComparable<T>
        {
            return value.CompareTo(other) > 0;
        }

        public static bool IsGreaterOrEqual<T>(this T value, T other) where T : IComparable<T>
        {
            return value.CompareTo(other) >= 0;
        }

        public static bool IsLessThan<T>(this T value, T other) where T : IComparable<T>
        {
            return value.CompareTo(other) < 0;
        }

        public static bool IsLessThanOrEqual<T>(this T value, T other) where T : IComparable<T>
        {
            return value.CompareTo(other) <= 0;
        }

        public static bool IsBetween<T>(this T value, T lower, T upper, bool inclusive) where T : IComparable<T>
        {
            if ((inclusive
                    ? lower.IsLessThanOrEqual(value)
                        ? 1
                        : 0
                    : lower.IsLessThan(value)
                        ? 1
                        : 0) ==
                0)
            {
                return false;
            }

            return !inclusive
                ? value.IsLessThan(upper)
                : value.IsLessThanOrEqual(upper);
        }
    }
}