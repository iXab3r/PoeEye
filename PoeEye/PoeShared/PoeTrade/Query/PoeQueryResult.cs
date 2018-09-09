using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQueryResult : IPoeQueryResult
    {
        private IPoeItem[] itemsList = new IPoeItem[0];

        public string Id { get; set; }

        public IPoeItem[] ItemsList
        {
            get => itemsList;
            set => itemsList = value ?? new IPoeItem[0];
        }
    }
}