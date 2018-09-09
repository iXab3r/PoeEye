using System;
using System.ComponentModel;
using System.Linq;

namespace PoeShared.Scaffolding.WPF
{
    public static class EnumHelper
    {
        public static EnumValueWithDescription[] GetValuesAndDescriptions(Type enumType)
        {
            var values = Enum.GetValues(enumType).Cast<object>();
            var valuesAndDescriptions = from value in values
                                        select new EnumValueWithDescription
                                        {
                                            Value = value,
                                            Description = value.GetType()
                                                               .GetMember(value.ToString())[0]
                                                               .GetCustomAttributes(true)
                                                               .OfType<DescriptionAttribute>()
                                                               .FirstOrDefault()?
                                                               .Description ?? value.ToString()
                                        };
            return valuesAndDescriptions.ToArray();
        }

        public static T SetFlags<T>(this T instance, T flagToSet) where T : struct
        {
            CheckIsEnum<T>(true);
            var value = Convert.ToUInt64(instance);
            var flag = Convert.ToUInt64(flagToSet);

            return (T)Enum.ToObject(typeof(T), value | flag);
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