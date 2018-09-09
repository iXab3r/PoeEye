using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using RestEase;
using Unity.Attributes;

namespace PoeShared.PoeDatabase.PoeNinja
{
    internal sealed class PoeNinjaDatabaseReader : DisposableReactiveObject, IPoeDatabaseReader, IPoeEconomicsSource
    {
        private static readonly string PoeNinjaDataUri = @"http://poe.ninja/api/Data/";
        private static readonly string StandardLeagueName = "Standard";

        private static readonly TimeSpan RequestsThrottling = TimeSpan.FromMilliseconds(1000);
        private readonly ISourceList<string> knownEntities = new SourceList<string>();
        private readonly ReadOnlyObservableCollection<string> knownEntityNames;

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture
        };

        public PoeNinjaDatabaseReader(
            [NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)]
            IScheduler bgScheduler)
        {
            Log.Instance.Debug("[PoeNinjaDatabaseReader..ctor] Created");

            knownEntities
                .Connect()
                .ObserveOn(uiScheduler)
                .Bind(out knownEntityNames)
                .Subscribe()
                .AddTo(Anchors);

            bgScheduler
                .Schedule(Initialize)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<string> KnownEntityNames => knownEntityNames;

        public IEnumerable<PoePrice> GetCurrenciesInChaosEquivalent(string leagueId)
        {
            return GetEconomics(leagueId);
        }

        private void Initialize()
        {
            var entities = GetEntities(StandardLeagueName);
            knownEntities.Clear();
            knownEntities.AddRange(entities);
        }

        private PoePrice[] GetEconomics(string leagueId)
        {
            Log.Instance.Debug($"[PoeNinjaDatabaseReader.Economics] Starting Economics API queries for league {leagueId}...");
            var api = RestClient.For<IPoeNinjaApi>(PoeNinjaDataUri, HandleRequestMessage, serializerSettings);
            var rawResult = api.GetCurrencyAsync(leagueId).Result;

            var result = rawResult.Lines
                                  .EmptyIfNull()
                                  .Select(x => new PoePrice(x.CurrencyTypeName, x.ChaosEquivalent))
                                  .Select(x => StringToPoePriceConverter.Instance.Convert(x.Price))
                                  .ToArray();

            Log.Instance.Debug($"[PoeNinjaDatabaseReader.Economics] All  done, {result.Length} name(s) found\n\t{result.DumpToTable()}");
            return result;
        }

        private string[] GetEntities(string leagueId)
        {
            Log.Instance.Debug($"[PoeNinjaDatabaseReader] Starting API queries...");
            try
            {
                var api = RestClient.For<IPoeNinjaApi>(PoeNinjaDataUri, HandleRequestMessage, serializerSettings);
                var result = new ConcurrentBag<string>();

                var sources = new[]
                {
                    ExtractFrom(api.GetMapsAsync(leagueId)),
                    ExtractFrom(api.GetDivinationCardsAsync(leagueId)),
                    ExtractFrom(api.GetEssenceAsync(leagueId)),
                    ExtractFrom(api.GetUniqueMapAsync(leagueId)),
                    ExtractFrom(api.GetUniqueJewelAsync(leagueId)),
                    ExtractFrom(api.GetUniqueFlaskAsync(leagueId)),
                    ExtractFrom(api.GetUniqueWeaponAsync(leagueId)),
                    ExtractFrom(api.GetUniqueArmourAsync(leagueId)),
                    ExtractFrom(api.GetUniqueAccessoryAsync(leagueId)),
                    ExtractFrom(api.GetCurrencyAsync(leagueId)),
                    ExtractFrom(api.GetFragmentOverviewAsync(leagueId))
                };
                foreach (var source in sources)
                {
                    try
                    {
                        var sourceResult = source.Result;
                        sourceResult.EmptyIfNull()
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ForEach(result.Add);
                    }
                    catch (Exception e)
                    {
                        Log.HandleException(e);
                    }
                }

                Log.Instance.Debug($"[PoeNinjaDatabaseReader] All done, {result.Count} name(s) found");

                return result.ToArray();
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                return new string[0];
            }
        }

        private async Task HandleRequestMessage(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Log.Instance.Debug($"[PoeNinjaDatabaseReader.Api] Requesting {request?.RequestUri}' (throttling: {RequestsThrottling})...");
            await Task.Delay(RequestsThrottling, cancellationToken);
        }

        private static async Task<string[]> ExtractFrom(Task<GenericResponse> task)
        {
            var result = await task;
            return result.Lines?.Select(x => x.Name).ToArray() ?? new string[0];
        }

        private static async Task<string[]> ExtractFrom(Task<CurrencyResponse> task)
        {
            var result = await task;
            return result.Lines?.Select(x => x.CurrencyTypeName).ToArray() ?? new string[0];
        }


        internal interface IPoeNinjaApi
        {
            [Get("GetMapOverview")]
            Task<GenericResponse> GetMapsAsync([Query] string league);

            [Get("GetDivinationCardsOverview")]
            Task<GenericResponse> GetDivinationCardsAsync([Query] string league);

            [Get("GetEssenceOverview")]
            Task<GenericResponse> GetEssenceAsync([Query] string league);

            [Get("GetFragmentOverview")]
            Task<CurrencyResponse> GetFragmentOverviewAsync([Query] string league);

            [Get("GetUniqueJewelOverview")]
            Task<GenericResponse> GetUniqueJewelAsync([Query] string league);

            [Get("GetUniqueMapOverview")]
            Task<GenericResponse> GetUniqueMapAsync([Query] string league);

            [Get("GetUniqueFlaskOverview")]
            Task<GenericResponse> GetUniqueFlaskAsync([Query] string league);

            [Get("GetUniqueWeaponOverview")]
            Task<GenericResponse> GetUniqueWeaponAsync([Query] string league);

            [Get("GetUniqueArmourOverview")]
            Task<GenericResponse> GetUniqueArmourAsync([Query] string league);

            [Get("GetUniqueAccessoryOverview")]
            Task<GenericResponse> GetUniqueAccessoryAsync([Query] string league);

            [Get("GetCurrencyOverview")]
            Task<CurrencyResponse> GetCurrencyAsync([Query] string league);
        }

        internal struct GenericResponse
        {
            public List<GenericItem> Lines { get; set; }
        }

        internal struct CurrencyResponse
        {
            public List<CurrencyItem> Lines { get; set; }
        }

        internal struct GenericItem
        {
            public string Name { get; set; }
        }

        internal struct CurrencyItem
        {
            public string CurrencyTypeName { get; set; }

            public float ChaosEquivalent { get; set; }
        }
    }
}