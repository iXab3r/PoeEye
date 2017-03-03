using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PoeShared.Scaffolding;
using RestEase;

namespace PoeShared.PoeDatabase.PoeNinja
{
    internal sealed class PoeNinjaDatabaseReader : DisposableReactiveObject, IPoeDatabaseReader
    {
        private static readonly string StandardLeagueName = "Standard";

        private readonly TaskCompletionSource<string[]> allKnownEntitiesSource;

        public PoeNinjaDatabaseReader()
        {
            Log.Instance.Debug("[PoeNinjaDatabaseReader..ctor] Created");
            allKnownEntitiesSource = new TaskCompletionSource<string[]>();

            Task.Factory.StartNew(RetrieveAllKnownEntitiesAsync).AddTo(Anchors);
        }

        public string[] KnownEntitiesNames => allKnownEntitiesSource.Task.Result;

        private void RetrieveAllKnownEntitiesAsync()
        {
            Log.Instance.Debug("[PoeNinjaDatabaseReader] Starting API queries...");
            try
            {
                var api = RestClient.For<IPoeNinjaApi>(@"http://poeninja.azureedge.net/api/Data/", async (request, cancellationToken) =>
                {
                    Log.Instance.Debug($"[PoeNinjaDatabaseReader.Api] Requesting {request?.RequestUri}'...");
                });

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
                };

                var knownEntities = new ConcurrentBag<string>();

                Parallel.ForEach(
                    sources, task =>
                    {
                        try
                        {
                            var sourceResult = task.Result;
                            sourceResult.ForEach(knownEntities.Add);
                        }
                        catch (Exception e)
                        {
                            Log.HandleException(e);
                        }
                    });

                Log.Instance.Debug($"[PoeNinjaDatabaseReader] All done, {knownEntities.Count} name(s) found");

                allKnownEntitiesSource.SetResult(knownEntities.ToArray());
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                allKnownEntitiesSource.TrySetResult(new string[0]);
            }
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