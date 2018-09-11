using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQueryResult : IPoeQueryResult
    {
        private IPoeItem[] itemsList = new IPoeItem[0];
        private IPoeQueryInfo query = PoeQueryInfo.Empty;

        public string Id { get; set; }

        public IPoeItem[] ItemsList
        {
            get => itemsList;
            set => itemsList = value ?? new IPoeItem[0];
        }

        public IPoeQueryInfo Query
        {
            get => query;
            set => query = value ?? PoeQueryInfo.Empty;
        }
    }
}