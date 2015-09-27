namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public class PoeQueryModArgument : PoeQueryArgumentBase, IPoeQueryModArgument
    {
        public PoeQueryModArgument([NotNull] string name) : base(name)
        {
        }

        public bool Excluded { get; set; }
    }
}