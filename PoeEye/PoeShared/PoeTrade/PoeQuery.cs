namespace PoeShared.PoeTrade
{
    public sealed class PoeQuery : IPoeQuery
    {
        private IPoeQueryArgument[] arguments = new IPoeQueryArgument[0];

        public IPoeQueryArgument[] Arguments
        {
            get { return arguments; }
            private set { arguments = value ?? new IPoeQueryArgument[0]; }
        }
    }
}