using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade.Query
{
    public sealed class PoeQueryResult : IPoeQueryResult
    {
        private readonly ReadOnlyDictionary<string, string> freeFormDataWrapper;
        private IPoeItem[] itemsList = new IPoeItem[0];
        private IPoeQueryInfo query = PoeQueryInfo.Empty;

        public PoeQueryResult() 
        {
            freeFormDataWrapper = new ReadOnlyDictionary<string, string>(FreeFormData);
        }
        
        public PoeQueryResult(IPoeQueryResult source) : this()
        {
            source.FreeFormData.ForEach(x => FreeFormData[x.Key] = x.Value);
            Id = source.Id;
            ItemsList = source.ItemsList.ToArray();
            Query = source.Query;
            ConvertedQuery = source.ConvertedQuery;
        }

        public IDictionary<string, string> FreeFormData { get; } = new Dictionary<string, string>();

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

        public string ConvertedQuery { get; set; }

        ReadOnlyDictionary<string, string> IPoeQueryResult.FreeFormData => freeFormDataWrapper;
    }
}