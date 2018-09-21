using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeEye.PathOfExileTrade.TradeApi.Domain;
using PoeShared;
using PoeShared.Scaffolding;
using RestEase;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    internal class PathOfExileTradePortalApiLimiter : IPathOfExileTradePortalApiLimiter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PathOfExileTradePortalApiLimiter));

        private static readonly HttpStatusCode HttpTooManyRequests = (HttpStatusCode)429;

        private static readonly ConcurrentDictionary<string, LimiterData> LimitsByPolicyName = new ConcurrentDictionary<string, LimiterData>();

        private readonly IPathOfExileTradePortalApi api;
        private readonly IClock clock;

        public PathOfExileTradePortalApiLimiter(
            [NotNull] IPathOfExileTradePortalApi api,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(api, nameof(api));
            Guard.ArgumentNotNull(clock, nameof(clock));
            this.api = api;
            this.clock = clock;
        }

        public Task<Response<JsonGetLeagueListResponse>> GetLeagueList()
        {
            return api.GetLeagueList();
        }

        public Task<Response<JsonGetStatsListResponse>> GetStatsList()
        {
            return api.GetStatsList();
        }

        public Task<Response<JsonGetStaticResponse>> GetStatic()
        {
            return api.GetStatic();
        }

        public Task<Response<JsonSearchRequest.Response>> Search(string league, JsonSearchRequest.Request query)
        {
            const string policyName = "SearchLimit";

            var response = LimitCall(policyName, () => api.Search(league, query));
            return response;
        }

        public Task<Response<JsonFetchRequest.Response>> FetchItems(string csvItemIdList, string queryId)
        {
            const string policyName = "FetchItemsLimit";

            var response = LimitCall(policyName, () => api.FetchItems(csvItemIdList, queryId));
            return response;
        }

        private async Task<Response<T>> LimitCall<T>(string policyName, Func<Task<Response<T>>> functor)
        {
            LimiterData before;
            lock (LimitsByPolicyName)
            {
                if (!LimitsByPolicyName.TryGetValue(policyName, out before))
                {
                    before = new LimiterData(policyName);
                    LimitsByPolicyName[policyName] = before;
                }
            }

            try
            {
                Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] Awaiting semaphore slot");

                await before.Semaphore.WaitAsync();
                return await LimitCall(before, functor);
            }
            finally
            {
                Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] Releasing semaphore slot");
                before.Semaphore.Release();
            }

        }

        private async Task<Response<T>> LimitCall<T>(LimiterData before, Func<Task<Response<T>>> functor)
        {
            const double targetFillRate = 0.8; 
            
            if (!before.Limit.IsEmpty)
            {
                Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] Current X-Rate limits: {before}");

                var now = clock.Now;
                var timeElapsed = now - before.Limit.TimeStamp;

                var sanctionsPeriod = before.Limit.SanctionsPeriod - timeElapsed;
                if (sanctionsPeriod > TimeSpan.Zero)
                {
                    Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] Applying sanctions throttling of {sanctionsPeriod}");
                    await Task.Delay(sanctionsPeriod);
                }
                else if (timeElapsed > before.Limit.TimePeriod)
                {
                    Log.Debug(
                        $"[PathOfExileTradePortalApi #{before.PolicyName}] Skipping throttling - too much time passed, elapsed: {timeElapsed}, rate period: {before.Limit.TimePeriod}");
                }
                else
                {
                    Log.Debug(
                        $"[PathOfExileTradePortalApi #{before.PolicyName}] Checking rate limits, elapsed: {timeElapsed}, rate period: {before.Limit.TimePeriod}");
                    var currentRate = before.Limit.Current / before.Limit.TimePeriod.TotalSeconds;
                    var maxRate = before.Limit.Max / before.Limit.TimePeriod.TotalSeconds;
                    var fillRate = currentRate / maxRate;

                    Log.Debug(
                        $"[PathOfExileTradePortalApi #{before.PolicyName}] Current rate: {currentRate:F2}, maxRate: {maxRate:F2} ({fillRate * 100:F2}%), limits: {before}");
                    if (fillRate > targetFillRate)
                    {
                        var diff = fillRate - targetFillRate;
                        var throttlePeriod = TimeSpan.FromSeconds(before.Limit.TimePeriod.TotalSeconds * diff * 2);
                        Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] Applying throttling of {throttlePeriod} (fillRate target: current: {fillRate}, target: {targetFillRate}, diff: {diff})");
                        await Task.Delay(throttlePeriod);
                    }
                }
            }

            Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] Executing request...");

            var sw = Stopwatch.StartNew();
            var result = await functor();
            sw.Stop();

            Log.Debug(
                $"[PathOfExileTradePortalApi #{before.PolicyName}] Request took {sw.ElapsedMilliseconds}ms, got HTTP {result.ResponseMessage.StatusCode} (success: {result.ResponseMessage.IsSuccessStatusCode})");

            var limits = new RateLimits(clock.Now, result.ResponseMessage);
            Log.Debug($"[PathOfExileTradePortalApi #{before.PolicyName}] X-Rate limits update, current: {before} => {limits}");
            before.Limit = limits;

            if (result.ResponseMessage.StatusCode == HttpTooManyRequests)
            {
                throw new ApplicationException("Too many requests sent, please lower the volume");
            }

            return result;
        }

        private struct RateLimits
        {
            public static readonly RateLimits Empty = new RateLimits();

            public bool IsEmpty => Current == 0 && Max == 0 && TimePeriod == default(TimeSpan);

            public long Current { get; }

            public long Max { get; }

            public TimeSpan TimePeriod { get; }

            public TimeSpan SanctionsPeriod { get; }

            public DateTime TimeStamp { get; }

            public string PolicyId { get; }

            public RateLimits(DateTime timestamp, HttpResponseMessage responseMessage)
            {
                TimeStamp = timestamp;
                if (!responseMessage.Headers.Contains("X-Rate-Limit-Rules"))
                {
                    PolicyId = "undefined";
                    Current = 0;
                    Max = 0;
                    TimePeriod = TimeSpan.Zero;
                    SanctionsPeriod = TimeSpan.Zero;
                    return;
                }

                PolicyId = responseMessage.Headers.GetValues("X-Rate-Limit-Policy").SingleOrDefault() ?? "Undefined";

                var registrations = responseMessage.Headers.GetValues("X-Rate-Limit-Rules")
                                                   .SingleOrDefault()
                                                   .SplitTrim(",");

                var limits = new List<RateLimit>();
                foreach (var paramName in registrations)
                {
                    var limit = new RateLimit {Name = paramName};

                    var limitRaw = responseMessage.Headers.GetValues($"X-Rate-Limit-{limit.Name}").SingleOrDefault();
                    limit.Limit = Trio.Parse(limitRaw);

                    var currentStateRaw = responseMessage.Headers.GetValues($"X-Rate-Limit-{limit.Name}-State").SingleOrDefault();
                    limit.CurrentState = Trio.Parse(currentStateRaw);

                    if (limit.Limit.IsEmpty)
                    {
                        continue;
                    }

                    limits.Add(limit);
                }

                Current = limits.Aggregate(0L, (current, limit) => Math.Max(current, limit.CurrentState.Value1));
                Max = limits.Aggregate(long.MaxValue, (current, limit) => Math.Min(current, limit.Limit.Value1));
                TimePeriod = TimeSpan.FromSeconds(limits.Aggregate(0L, (current, limit) => Math.Max(current, limit.Limit.Value2)));
                SanctionsPeriod = TimeSpan.FromSeconds(limits.Aggregate(0L, (current, limit) => Math.Max(current, limit.CurrentState.Value3)));
            }

            public override string ToString()
            {
                return this.DumpToTextRaw();
            }
        }

        private class LimiterData
        {
            private long requestId;

            public LimiterData(string policyName)
            {
                PolicyName = policyName;
            }

            public string PolicyName { get; }
            public RateLimits Limit { get; set; }

            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(initialCount:1, maxCount:1);

            public long RequestId => requestId;

            public long IncrementRequestId()
            {
                return Interlocked.Increment(ref requestId);
            }

            public override string ToString()
            {
                return $"#{requestId} {Limit}";
            }
        }

        private struct RateLimit
        {
            public string Name { get; set; }

            public Trio Limit { get; set; }

            public Trio CurrentState { get; set; }
        }

        private struct Trio
        {
            public static readonly Trio Empty = new Trio();

            public bool IsEmpty => Empty.Equals(this);

            public long Value1 { get; set; }
            public long Value2 { get; set; }
            public long Value3 { get; set; }

            private static readonly Regex Parser = new Regex("(?<first>\\d+)?:(?<second>\\d+)?:(?<third>\\d+)");

            public static Trio Parse(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Empty;
                }

                var match = Parser.Match(value);
                if (!match.Success)
                {
                    return Empty;
                }

                var result = new Trio();
                long.TryParse(match.Groups["first"].Value, out var firstValue);
                result.Value1 = firstValue;

                long.TryParse(match.Groups["second"].Value, out var secondValue);
                result.Value2 = secondValue;

                long.TryParse(match.Groups["third"].Value, out var thirdValue);
                result.Value3 = thirdValue;

                return result;
            }

            public override string ToString()
            {
                return $"{Value1}:{Value2}:{Value3}";
            }
        }
    }
}