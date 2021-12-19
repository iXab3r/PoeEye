using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using log4net;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF
{
    public static class EnumHelper
    {
        private static readonly IFluentLog Log = typeof(EnumHelper).PrepareLogger();

        public static TEnum ParseEnumSafe<TEnum>(this string instance, TEnum defaultValue = default(TEnum)) where TEnum : struct
        {
            if (Enum.TryParse(instance, out TEnum result))
            {
                return result;
            }

            Log.Warn($"Failed to parse enum value '{instance}'(isEmpty: {string.IsNullOrEmpty(instance)}), defaulting to {defaultValue}");
            result = defaultValue;

            return result;
        }

        public static EnumValueWithDescription[] GetValuesAndDescriptions(Type enumType, string defaultValueName)
        {
            var values = Enum.GetValues(enumType).Cast<object>().ToHashSet();
            var membersByName = enumType.GetMembers().OfType<FieldInfo>().Where(x => x.IsStatic).ToDictionary(x => x.Name, x => x);
            var defaultValue = enumType.GetDefault();
            values.Add(defaultValue);

            string GetDescriptionOrDefault(object value, Func<string> defaultDescriptionFactory)
            {
                var memberName = value.ToString() ?? string.Empty;
                if (!membersByName.TryGetValue(memberName, out var member))
                {
                    return defaultDescriptionFactory();
                }

                var descriptionAttribute = member
                    .GetCustomAttributes(true)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault();
                return descriptionAttribute == default ? defaultDescriptionFactory() : descriptionAttribute.Description;
            }

            bool GetBrowsableOrDefault(object value, Func<bool> defaultDescriptionFactory)
            {
                var memberName = value.ToString() ?? string.Empty;
                if (!membersByName.TryGetValue(memberName, out var member))
                {
                    return defaultDescriptionFactory();
                }

                var descriptionAttribute = member
                    .GetCustomAttributes(true)
                    .OfType<BrowsableAttribute>()
                    .FirstOrDefault();
                return descriptionAttribute?.Browsable ?? defaultDescriptionFactory();
            }
            
            var valuesAndDescriptions = from value in values.OrderBy(x => x)
                select new
                {
                    Value = value,
                    Description = GetDescriptionOrDefault(value, () => defaultValue.Equals(value) ? defaultValueName : value.ToString()),
                    Browsable = GetBrowsableOrDefault(value, () => true)
                };
            
            return valuesAndDescriptions
                .Where(x => x.Browsable)
                .Select(x => new EnumValueWithDescription
                {
                    Value = x.Value,
                    Description = x.Description
                })
                .ToArray();
        }
        
        public static EnumValueWithDescription[] GetValuesAndDescriptions(Type enumType)
        {
            return GetValuesAndDescriptions(enumType, "Default");
        }

        public static T SetFlags<T>(this T instance, T flagToSet) where T : struct
        {
            CheckIsEnum<T>(true);
            var value = Convert.ToUInt64(instance);
            var flag = Convert.ToUInt64(flagToSet);

            return (T) Enum.ToObject(typeof(T), value | flag);
        }

        private static void CheckIsEnum<T>(bool withFlags)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));
            }

            if (withFlags && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));
            }
        }

        public static string GetDescription<T>(this T value) where T : struct
        {
            CheckIsEnum<T>(false);
            var name = Enum.GetName(typeof(T), value);
            if (name == null)
            {
                return null;
            }

            var field = typeof(T).GetField(name);
            if (field == null)
            {
                return null;
            }

            var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attr?.Description;
        }

        public struct EnumValueWithDescription
        {
            public object Value { get; set; }

            public string Description { get; set; }
        }
    }
}