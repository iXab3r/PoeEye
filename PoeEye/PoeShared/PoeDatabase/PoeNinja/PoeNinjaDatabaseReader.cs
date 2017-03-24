using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using RestEase;

namespace PoeShared.PoeDatabase.PoeNinja
{
    internal sealed class PoeNinjaDatabaseReader : DisposableReactiveObject, IPoeDatabaseReader
    {
        private static readonly string PoeNinjaDataUri = @"http://poeninja.azureedge.net/api/Data/";
        private static readonly string StandardLeagueName = "Standard";

        private static readonly TimeSpan RequestsThrottling = TimeSpan.FromMilliseconds(1000);
        private readonly ISourceList<string> knownEntities = new SourceList<string>();
        private readonly ReadOnlyObservableCollection<string> knownEntityNames;

        public PoeNinjaDatabaseReader(
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Log.Instance.Debug("[PoeNinjaDatabaseReader..ctor] Created");

            knownEntities
                .Connect()
                .Bind(out knownEntityNames)
                .Subscribe()
                .AddTo(Anchors);

            bgScheduler
                .Schedule(Initialize)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<string> KnownEntityNames => knownEntityNames;

        private void Initialize()
        {
            var entities = GetEntities();
            knownEntities.Clear();
            knownEntities.AddRange(entities);
        }

        private string[] GetEntities()
        {
            Log.Instance.Debug("[PoeNinjaDatabaseReader] Starting API queries...");
            try
            {
                var api = RestClient.For<IPoeNinjaApi>(PoeNinjaDataUri, HandleRequestMessage);
                var result = new ConcurrentBag<string>();
                
                var sources = new[]
                {
                    ExtractFrom(api.GetMapsAsync(StandardLeagueName)),
                    ExtractFrom(api.GetDivinationCardsAsync(StandardLeagueName)),
                    ExtractFrom(api.GetEssenceAsync(StandardLeagueName)),
                    ExtractFrom(api.GetUniqueMapAsync(StandardLeagueName)),
                    ExtractFrom(api.GetUniqueJewelAsync(StandardLeagueName)),
                    ExtractFrom(api.GetUniqueFlaskAsync(StandardLeagueName)),
                    ExtractFrom(api.GetUniqueWeaponAsync(StandardLeagueName)),
                    ExtractFrom(api.GetUniqueArmourAsync(StandardLeagueName)),
                    ExtractFrom(api.GetUniqueAccessoryAsync(StandardLeagueName)),
                    ExtractFrom(api.GetCurrencyAsync(StandardLeagueName)),
                    ExtractFrom(api.GetFragmentOverviewAsync(StandardLeagueName)),
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
        }                                                   
    }
}