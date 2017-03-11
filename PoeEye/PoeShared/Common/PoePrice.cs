using System;

namespace PoeShared.Common
{
    public struct PoePrice : IComparable
    {
        public static readonly PoePrice Empty = new PoePrice(KnownCurrencyNameList.Unknown, 0f);

        public PoePrice(string currencyType, float value)
        {
            CurrencyType = currencyType;
            Value = value;

            Price = currencyType != KnownCurrencyNameList.Unknown 
                ? $"{value} {currencyType}" 
                : $"{currencyType}";
        }

        public string CurrencyType { get; private set; }

        public float Value { get; private set; }

        public string Price { get; }

        public bool IsEmpty => Empty.Equals(this);

        public override string ToString()
        {
            return Price;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is PoePrice))
            {
                return 1;
            }

            var other = (PoePrice) obj;
            return Value.CompareTo(other.Value);
        }
    }
}