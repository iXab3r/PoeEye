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

        public string CurrencyType { get; }

        public float Value { get; }

        public bool HasValue => Value > 0;

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

        public bool Equals(PoePrice other)
        {
            return string.Equals(CurrencyType, other.CurrencyType) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is PoePrice && Equals((PoePrice) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((CurrencyType != null ? CurrencyType.GetHashCode() : 0) * 397) ^ Value.GetHashCode();
            }
        }
    }
}