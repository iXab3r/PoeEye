using JetBrains.Annotations;

namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQueryFloatArgument : PoeQueryArgumentBase, IPoeQueryFloatArgument
    {
        public PoeQueryFloatArgument([NotNull] string name, float value) : base(name)
        {
            Value = value;
        }

        public float Value { get; }
    }
}