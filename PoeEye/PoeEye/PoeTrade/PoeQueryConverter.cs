namespace PoeEye.PoeTrade
{
    using System.Collections.Generic;

    using Guards;

    using PoeShared.PoeTrade;

    using TypeConverter;

    internal sealed class PoeQueryConverter : IConverter<IPoeQuery, IDictionary<string, object>>
    {
        public IDictionary<string, object> Convert(IPoeQuery value)
        {
            Guard.ArgumentNotNull(() => value);
            
            var queryPostData = new Dictionary<string, object>
            {
                {"league", "Warbands"},
                {"name", "Temple map"}
            };
            return queryPostData;
        }
    }
}