namespace PoeShared.PoeTrade.Query
{
    using Common;

    public sealed class PoeQueryResult : IPoeQueryResult
    {
        private IPoeItem[] itemsList = new IPoeItem[0];

        public string Id { get; set; }

        public IPoeItem[] ItemsList
        {
            get { return itemsList; }
            set { itemsList = value ?? new IPoeItem[0]; }
        }
    }
}