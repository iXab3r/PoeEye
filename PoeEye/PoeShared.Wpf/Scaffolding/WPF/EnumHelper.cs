using System;
using System.ComponentModel;
using System.Linq;
using log4net;

namespace PoeShared.Scaffolding.WPF
{
    public static class EnumHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EnumHelper));

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

        public static EnumValueWithDescription[] GetValuesAndDescriptions(Type enumType)
        {
            var values = Enum.GetValues(enumType).Cast<object>();
            var valuesAndDescriptions = from value in values
                select new
                {
                    Value = value,
                    Description = value.GetType()
                                      .GetMember(value.ToString())[0]
                                      .GetCustomAttributes(true)
                                      .OfType<DescriptionAttribute>()
                                      .FirstOrDefault()?
                                      .Description ?? value.ToString(),
                    Browsable = value.GetType()
                                    .GetMember(value.ToString())[0]
                                    .GetCustomAttributes(true)
                                    .OfType<BrowsableAttribute>()
                                    .FirstOrDefault()?
                                    .Browsable ?? true
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