using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;

namespace PoeEye.ExileToolsApi.Prism
{
    internal sealed class PoeQueryInfoToPoeQueryConverter : IConverter<IPoeQueryInfo, IPoeQuery>
    {
        public IPoeQuery Convert(IPoeQueryInfo value)
        {
            return new PoeQuery();
        }
    }
}