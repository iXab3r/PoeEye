namespace PoeShared.PoeTrade.Query
{
    using Guards;

    public sealed class PoeQueryStringArgument : PoeQueryArgumentBase, IPoeQueryStringArgument
    {
        public PoeQueryStringArgument(string name, string value) : base(name)
        {
            Value = value;
        }

        public string Value { get; }
    }
}