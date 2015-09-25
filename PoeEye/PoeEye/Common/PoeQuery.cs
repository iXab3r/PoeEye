namespace PoeEye.Common
{
    using PoeShared.Query;

    internal sealed class PoeQuery : IPoeQuery
    {
        public IPoeQueryArgument[] Arguments { get; private set; }
    }
}