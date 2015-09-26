namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public class PoeQueryModArgument : IPoeQueryModArgument
    {
        public PoeQueryModArgument([NotNull] string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public bool Excluded { get; set; }
    }
}