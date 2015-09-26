namespace PoeShared.PoeTrade.Query
{
    using Guards;

    public sealed class PoeQueryStringArgument : IPoeQueryStringArgument
    {
        public PoeQueryStringArgument(string name, string value) 
        {
            Guard.ArgumentNotNullOrEmpty(() => name);
            Guard.ArgumentNotNullOrEmpty(() => value);

            Name = name;
            Value = value;
        }

        public string Value { get; }

        public string Name { get; }
    }
}