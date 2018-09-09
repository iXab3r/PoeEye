namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQuery : IPoeQuery
    {
        private IPoeQueryArgument[] arguments = new IPoeQueryArgument[0];

        public IPoeQueryArgument[] Arguments
        {
            get => arguments;
            set => arguments = value ?? new IPoeQueryArgument[0];
        }
    }
}