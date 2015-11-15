namespace PoeShared.Common
{
    public class PoePrice
    {
        public PoePrice(string currencyType, float value)
        {
            CurrencyType = currencyType;
            Value = value;
        }

        public string CurrencyType { get; private set; }

        public float Value { get; private set; }
    }
}