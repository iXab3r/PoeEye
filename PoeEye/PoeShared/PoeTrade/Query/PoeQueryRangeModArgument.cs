namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public class PoeQueryRangeModArgument : PoeQueryModArgument, IPoeQueryRangeModArgument
    {
        public PoeQueryRangeModArgument([NotNull] string name) : base(name)
        {
        }

        public float? Min { get; set; }

        public float? Max { get; set; }
    }
}