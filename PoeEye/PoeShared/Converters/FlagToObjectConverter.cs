using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoeShared.Converters
{
    public class FlagToObjectConverter : IValueConverter
    {
        public object BitMask { get; set; }
        
        public object TrueValue { get; set; }
        
        public object FalseValue { get; set; }

        public object Convert(object rawValue, Type targetType, object parameter, CultureInfo culture)
        {
            if (BitMask == null)
            {
                return Binding.DoNothing;
            }
            if (rawValue == null)
            {
                return Binding.DoNothing;
            }

            var mask = ToUInt64(BitMask);
            var value = ToUInt64(rawValue);

            return IsFlagSet(value, mask) ? TrueValue : FalseValue;
        }
        
        public object ConvertBack(object rawValue, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        
        internal static ulong ToUInt64(object value)
        {
            var typeCode = System.Convert.GetTypeCode(value);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return System.Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return (ulong) System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentException($"Invalid typecode: {typeCode}");
            }
        }
        
        internal static bool IsFlagSet(ulong value, ulong flag)
        {
            return (value & flag) != 0;
        }
    }

    public sealed class FlagToVisibilityConverter : FlagToObjectConverter
    {
        public new Visibility TrueValue
        {
            get { return (Visibility)base.TrueValue; }
            set { base.TrueValue = value; }
        }
        
        public new Visibility FalseValue
        {
            get { return (Visibility)base.FalseValue; }
            set { base.FalseValue = value; }
        }
    }
}