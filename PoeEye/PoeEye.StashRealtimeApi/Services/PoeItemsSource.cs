using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Anotar.Log4Net;
using Guards;
using JetBrains.Annotations;
using PoeEye.StashRealtimeApi.API;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using RestEase;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeItemsSource : DisposableReactiveObject, IPoeItemsSource
    {
        private static readonly TimeSpan AllowedPollingInterval = TimeSpan.FromMilliseconds(1100);

        private readonly IFactory<IPoeItem, IStashItem, StashTab> poeItemFactory;
        private readonly IClock clock;
        private static readonly string StashApiUri = @"http://www.pathofexile.com/api";

        private readonly IStashApi client;
        private readonly ISubject<IPoeItem[]> items = new Subject<IPoeItem[]>();

        private readonly BlockingCollection<List<StashTab>> rawPacks = new BlockingCollection<List<StashTab>>();

        private string nextChangeId;
        private DateTime lastRequestTimestamp;

        public PoeItemsSource(
            [NotNull] IFactory<IPoeItem, IStashItem, StashTab> poeItemFactory,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(poeItemFactory, nameof(poeItemFactory));

            this.poeItemFactory = poeItemFactory;
            this.clock = clock;

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

            var consumerCancellationTokenSource = new CancellationTokenSource();
            var consumerThread = new Task(RawItemsConsumerThread, consumerCancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            consumerThread.Start();

            Disposable
                .Create(consumerCancellationTokenSource.Cancel)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, AllowedPollingInterval)
                .Subscribe(HandleUpdateRequest)
                .AddTo(Anchors);
        }

        public IObservable<IPoeItem[]> ItemPacks => items;

        private string GetStartingIdFromPoeRates()
        {
            LogTo.Debug($"Requesting lastChangeId from poe-rates.com ...");
            var poeRatesApi = RestClient.For<IPoeRatesApi>("http://poe-rates.com/actions/");
            var result = poeRatesApi.GetLastChangeId().Result;

            return result.GetContent().ChangeId;
        }

        private string GetStartingIdFromPoeNinja()
        {
            LogTo.Debug($"Requesting lastChangeId from Poe.ninja ...");
            var api = RestClient.For<IPoeNinjaApi>("http://api.poe.ninja/api/Data");
            var result = api.GetStats().Result;

            return result.GetContent().NextChangeId;
        }

        private void RawItemsConsumerThread(object cancellationTokenUntyped)
        {
            try
            {
                LogTo.Debug("Thread started");
                var cancellationToken = (CancellationToken) cancellationTokenUntyped;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var nextPack = rawPacks.Take(cancellationToken);
                    if (nextPack == null || nextPack.Count == 0)
                    {
                        LogTo.Warn("Something went wrong - current pack is null or empty");
                        continue;
                    }

                    LogTo.Debug($"Processed next pack of {nextPack.Count} items");

                    var sw = Stopwatch.StartNew();
                    var poeItems = ToItems(nextPack).ToArray();
                    var itemsToAdd = poeItems.Where(x => !string.IsNullOrWhiteSpace(x.Hash)).ToArray();
                    sw.Stop();

                    LogTo.Debug($"Processed {poeItems.Length} item(s) in {sw.ElapsedMilliseconds}ms. Found {poeItems.Length - itemsToAdd.Length} bad items");

                    items.OnNext(itemsToAdd);
                }
                LogTo.Debug("Cancellation requested");
            }
            catch (OperationCanceledException)
            {
                LogTo.Warn($"Operation cancelled");
            }
            catch (Exception e)
            {
                LogTo.ErrorException($"Exception occurred in consumer thread", e);
            }
            finally
            {
                LogTo.Debug("Thread completed");
            }
        }

        private void HandleUpdateRequest()
        {
            try
            {
                if (lastRequestTimestamp != DateTime.MinValue)
                {
                    var timeElapsed = clock.Now - lastRequestTimestamp;
                    var timeToSleep = AllowedPollingInterval - timeElapsed;
                    LogTo.Debug($"Update request received, time elapsed since last update: {timeElapsed.TotalMilliseconds}ms, timeToSleep: {timeToSleep.TotalMilliseconds}ms");
                    if (timeElapsed < AllowedPollingInterval)
                    {
                        LogTo.Debug($"Awaiting for {timeToSleep.TotalMilliseconds}ms");
                        Thread.Sleep(timeToSleep);
                    }
                }

                LogTo.Debug($"Requesting updated data...");
                var sw = Stopwatch.StartNew();

                if (string.IsNullOrWhiteSpace(nextChangeId))
                {
                    nextChangeId = GetStartingIdFromPoeNinja();
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
            finally
            {
                lastRequestTimestamp = clock.Now;
            }
        }

        private void HandleResponse(StashApiResponse response)
        {
            LogTo.Debug($"Processing response, stashes count: {response.Stashes?.Count ?? -1}, proposed nextChangeId: {response.NextChangeId}");

           
            if (response.Stashes == null || response.Stashes.Count == 0)
            {
                LogTo.Warn($"Empty response, we should re-request with the same {nextChangeId} instead of proposed {response.NextChangeId}");
            }
            else
            {
                nextChangeId = response.NextChangeId;
                LogTo.Debug($"Adding pack of {response.Stashes.Count} items to a processing queue, currently there are {rawPacks.Count} elements");
                rawPacks.Add(response.Stashes);
            }

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
