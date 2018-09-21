﻿using System;
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
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeEye.StashRealtimeApi.API;
using PoeShared;
using PoeShared.Common;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using RestEase;

namespace PoeEye.StashRealtimeApi.Services
{
    internal sealed class PoeItemsSource : DisposableReactiveObject, IPoeItemsSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeItemsSource));

        private static readonly TimeSpan AllowedPollingInterval = TimeSpan.FromMilliseconds(1100);
        private static readonly string StashApiUri = @"http://www.pathofexile.com/api";

        private readonly IStashApi client;
        private readonly IClock clock;
        private readonly ISubject<IPoeItem[]> items = new Subject<IPoeItem[]>();

        private readonly IFactory<IPoeItem, IStashItem, StashTab> poeItemFactory;

        private readonly BlockingCollection<List<StashTab>> rawPacks = new BlockingCollection<List<StashTab>>();
        private DateTime lastRequestTimestamp;

        private string nextChangeId;

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
                BaseAddress = new Uri(StashApiUri)
            };

            client = new RestClient(httpClient).For<IStashApi>();

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
            Log.Debug($"Requesting lastChangeId from poe-rates.com ...");
            var poeRatesApi = RestClient.For<IPoeRatesApi>("http://poe-rates.com/actions/");
            var result = poeRatesApi.GetLastChangeId().Result;

            return result.GetContent().ChangeId;
        }

        private string GetStartingIdFromPoeNinja()
        {
            Log.Debug($"Requesting lastChangeId from Poe.ninja ...");
            var api = RestClient.For<IPoeNinjaApi>("http://api.poe.ninja/api/Data");
            var result = api.GetStats().Result;

            return result.GetContent().NextChangeId;
        }

        private void RawItemsConsumerThread(object cancellationTokenUntyped)
        {
            try
            {
                Log.Debug("Thread started");
                var cancellationToken = (CancellationToken)cancellationTokenUntyped;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var nextPack = rawPacks.Take(cancellationToken);
                    if (nextPack == null || nextPack.Count == 0)
                    {
                        Log.Warn("Something went wrong - current pack is null or empty");
                        continue;
                    }

                    Log.Debug($"Processed next pack of {nextPack.Count} items");

                    var sw = Stopwatch.StartNew();
                    var poeItems = ToItems(nextPack).ToArray();
                    var itemsToAdd = poeItems.Where(x => !string.IsNullOrWhiteSpace(x.Hash)).ToArray();
                    sw.Stop();

                    Log.Debug(
                        $"Processed {poeItems.Length} item(s) in {sw.ElapsedMilliseconds}ms. Found {poeItems.Length - itemsToAdd.Length} bad items");

                    items.OnNext(itemsToAdd);
                }

                Log.Debug("Cancellation requested");
            }
            catch (OperationCanceledException)
            {
                Log.Warn($"Operation cancelled");
            }
            catch (Exception e)
            {
                Log.Error($"Exception occurred in consumer thread", e);
            }
            finally
            {
                Log.Debug("Thread completed");
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
                    Log.Debug(
                        $"Update request received, time elapsed since last update: {timeElapsed.TotalMilliseconds}ms, timeToSleep: {timeToSleep.TotalMilliseconds}ms");
                    if (timeElapsed < AllowedPollingInterval)
                    {
                        Log.Debug($"Awaiting for {timeToSleep.TotalMilliseconds}ms");
                        Thread.Sleep(timeToSleep);
                    }
                }

                Log.Debug($"Requesting updated data...");
                var sw = Stopwatch.StartNew();

                if (string.IsNullOrWhiteSpace(nextChangeId))
                {
                    nextChangeId = GetStartingIdFromPoeNinja();
                    Log.Debug($"Starting changeId: {nextChangeId}");
                }

                var response = client.PublicStashTabs(nextChangeId).Result;
                sw.Stop();
                Log.Debug($"Got HTTP response(in {sw.ElapsedMilliseconds}ms): {response?.ResponseMessage?.StatusCode}");
                if (response == null || response.ResponseMessage == null)
                {
                    Log.Warn($"Got null response for change id {nextChangeId} !");
                }
                else
                {
                    if (!response.ResponseMessage.IsSuccessStatusCode)
                    {
                        Log.Warn($"Got unsucessfull response:\n{response.ResponseMessage.DumpToText()}");
                    }
                    else
                    {
                        HandleResponse(response.GetContent());
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to get next change with Id {nextChangeId}", e);
            }
            finally
            {
                lastRequestTimestamp = clock.Now;
            }
        }

        private void HandleResponse(StashApiResponse response)
        {
            Log.Debug($"Processing response, stashes count: {response.Stashes?.Count ?? -1}, proposed nextChangeId: {response.NextChangeId}");


            if (response.Stashes == null || response.Stashes.Count == 0)
            {
                Log.Warn($"Empty response, we should re-request with the same {nextChangeId} instead of proposed {response.NextChangeId}");
            }
            else
            {
                nextChangeId = response.NextChangeId;
                Log.Debug($"Adding pack of {response.Stashes.Count} items to a processing queue, currently there are {rawPacks.Count} elements");
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