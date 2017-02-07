using System;
using System.ComponentModel;
using System.Linq;

namespace PoeShared.Scaffolding.WPF
{
    public class EnumHelper
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
                                                    .First()
                                                    .Description
                                        };
            return valuesAndDescriptions.ToArray();
        }

        public struct EnumValueWithDescription
        {
            public object Value { get; set; }

            public string Description { get; set; }
        }
    }
}