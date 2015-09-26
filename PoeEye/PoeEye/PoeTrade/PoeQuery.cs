namespace PoeEye.PoeTrade
{
    using PoeShared.PoeTrade;

    internal sealed class PoeQuery : IPoeQuery
    {
        public IPoeQueryArgument[] Arguments { get; private set; }
    }
}