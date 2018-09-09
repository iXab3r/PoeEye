using JetBrains.Annotations;

namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQueryIntArgument : PoeQueryArgumentBase, IPoeQueryIntArgument
    {
        public PoeQueryIntArgument([NotNull] string name, int value) : base(name)
        {
            Value = value;
        }

        public int Value { get; }
    }
}