using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryResult
    {
        string Id { [CanBeNull] get; }

        IPoeItem[] ItemsList { [NotNull] get; }
        
        IPoeQueryInfo Query { [CanBeNull] get; }
        
        ReadOnlyDictionary<string, string> FreeFormData { get; }
    }
}