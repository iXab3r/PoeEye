namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public sealed class PoeQueryIntArgument : PoeQueryArgumentBase, IPoeQueryIntArgument
    {
        public PoeQueryIntArgument([NotNull] string name, int value) : base(name)
        {
            Value = value;
        }

        public int Value { get; }
    }
}