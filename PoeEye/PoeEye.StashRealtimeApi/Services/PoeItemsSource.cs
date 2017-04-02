using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Anotar.Log4Net;
using Guards;
using JetBrains.Annotations;
using PoeEye.StashRealtimeApi.API;
using PoeShared.Common;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using RestEase;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeItemsSource : DisposableReactiveObject, IPoeItemsSource
    {
        private readonly IFactory<IPoeItem, IStashItem, StashTab> poeItemFactory;
        private static readonly string StashApiUri = @"https://www.pathofexile.com/api";

        private readonly IStashApi client;
        private readonly ISubject<IPoeItem[]> items = new Subject<IPoeItem[]>();

        private string nextChangeId;

        public PoeItemsSource([NotNull]  IFactory<IPoeItem, IStashItem, StashTab> poeItemFactory)
        {
            Guard.ArgumentNotNull(() => poeItemFactory);

            this.poeItemFactory = poeItemFactory;


            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(StashApiUri),
            };

            client =  new RestClient(httpClient)
            {
            }.For<IStashApi>();

            Observable
                .Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1))
                .Subscribe(HandleUpdateRequest)
                .AddTo(Anchors);
        }

        public IObservable<IPoeItem[]> ItemPacks => items;

        private string GetStartingId()
        {
            LogTo.Debug($"Requesting lastChangeId...");
            var poeRatesApi = RestClient.For<IPoeRatesApi>("http://poe-rates.com/actions/");
            var result = poeRatesApi.GetLastChangeId().Result;

            return result.GetContent().ChangeId;
        }

        private void HandleUpdateRequest()
        {
            LogTo.Debug($"Requesting updated data...");
            try
            {
                var sw = Stopwatch.StartNew();

                if (string.IsNullOrWhiteSpace(nextChangeId))
                {
                    nextChangeId = GetStartingId();
                    LogTo.Debug($"Starting changeId: {nextChangeId}");
                }

                var response = client.PublicStashTabs(nextChangeId).Result;
                sw.Stop();
                LogTo.Debug($"Got HTTP response(in {sw.ElapsedMilliseconds}ms): {response?.ResponseMessage?.StatusCode}");
                if (response == null || response.ResponseMessage == null)
                {
                    LogTo.Warn($"Got null response for change id {nextChangeId} !");
                }
                else
                {
                    if (!response.ResponseMessage.IsSuccessStatusCode)
                    {
                        LogTo.Warn($"Got unsucessfull response:\n{response.ResponseMessage.DumpToText()}");
                    }
                    else
                    {
                        HandleResponse(response.GetContent());
                    }
                }
            }
            catch (Exception e)
            {
                LogTo.ErrorException($"Failed to get next change with Id {nextChangeId}", e);
            }
        }

        private void HandleResponse(StashApiResponse response)
        {
            LogTo.Debug($"Processing response, stashes count: {response.Stashes?.Count ?? -1}, nextChangeId: {response.NextChangeId}");
            nextChangeId = response.NextChangeId;

            var sw = Stopwatch.StartNew();
            var poeItems = ToItems(response.Stashes.EmptyIfNull()).ToArray();
            var itemsToAdd = poeItems.Where(x => !string.IsNullOrWhiteSpace(x.Hash)).ToArray();
            sw.Stop();

            LogTo.Debug($"Processed {poeItems.Length} item(s) in {sw.ElapsedMilliseconds}ms. Found {poeItems.Length - itemsToAdd.Length}");

            //items.OnNext(itemsToAdd);
        }

        private IEnumerable<IPoeItem> ToItems(IEnumerable<StashTab> stashes)
        {
            foreach (var stashTab in stashes)
            {
                foreach (var stashTabItem in stashTab.Items)
                {
                    var poeItem = poeItemFactory.Create(stashTabItem, stashTab);
                    yield return poeItem;
                }
            }
        }
    }
}