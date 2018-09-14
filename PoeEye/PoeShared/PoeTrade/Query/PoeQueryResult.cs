using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQueryResult : IPoeQueryResult
    {
        private IPoeItem[] itemsList = new IPoeItem[0];
        private IPoeQueryInfo query = PoeQueryInfo.Empty;
        private readonly IDictionary<string,string> freeFormData = new Dictionary<string, string>();
        private readonly ReadOnlyDictionary<string, string> freeFormDataWrapper;

        public string Id { get; set; }

        public PoeQueryResult()
        {
            freeFormDataWrapper = new ReadOnlyDictionary<string, string>(freeFormData);
        }

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

        public IDictionary<string, string> FreeFormData => freeFormData;

        ReadOnlyDictionary<string, string> IPoeQueryResult.FreeFormData => this.freeFormDataWrapper;
    }
}