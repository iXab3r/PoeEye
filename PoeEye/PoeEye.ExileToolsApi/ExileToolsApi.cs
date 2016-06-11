using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Nest;
using Newtonsoft.Json.Linq;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.Entities;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;

namespace PoeEye.ExileToolsApi
{
    internal sealed class ExileToolsApi : IPoeApi
    {
        private readonly IConverter<IPoeQueryInfo, ISearchRequest> queryConverter;
        private readonly IConverter<ItemConversionInfo, IPoeItem> poeItemConverter;
        private readonly ExileToolsSource exileSource;

        public ExileToolsApi(
            [NotNull] IConverter<IPoeQueryInfo, ISearchRequest> queryConverter,
            [NotNull] IConverter<ItemConversionInfo, IPoeItem> poeItemConverter,
            [NotNull] ExileToolsSource exileSource)
        {
            Guard.ArgumentNotNull(() => queryConverter);
            Guard.ArgumentNotNull(() => poeItemConverter);
            Guard.ArgumentNotNull(() => exileSource);

            this.queryConverter = queryConverter;
            this.poeItemConverter = poeItemConverter;
            this.exileSource = exileSource;
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            return Observable
                .Start(() => IssueQueryInternal(query), Scheduler.Default)
                .ToTask();
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            return Observable
                 .Start(exileSource.LoadStaticData, Scheduler.Default)
                 .ToTask();
        }


        private IPoeQueryResult IssueQueryInternal(IPoeQueryInfo queryInfo)
        {
            var query = queryConverter.Convert(queryInfo);

            var queryResult = exileSource.Client.Search<JRaw>(query);

            Log.Instance.Debug($"[ExileToolsApi] Response data:\n{queryResult.DebugInformation}");

            var convertedItems = queryResult
                .Hits
                .Select(x => new ItemConversionInfo(x.Source, ExtractModsList(queryInfo)))
                .Select(poeItemConverter.Convert)
                .Where(x => x != null)
                .ToArray();
            return new PoeQueryResult()
            {
                ItemsList = convertedItems,
            };
        }

        private string[] ExtractModsList(IPoeQueryInfo queryInfo)
        {
            return queryInfo.ModGroups.SelectMany(x => x.Mods).Select(x => x.Name).ToArray();
        }
    }
}
