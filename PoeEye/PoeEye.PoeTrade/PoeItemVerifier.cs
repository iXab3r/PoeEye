using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Communications;
using PoeShared.PoeTrade;
using PoeShared.Prism;

namespace PoeEye.PoeTrade
{
    internal sealed class PoeItemVerifier : IPoeItemVerifier
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeItemVerifier));

        private static readonly string VerificationUri = "http://verify.xyz.is/";
        private readonly IFactory<IHttpClient> clientsFactory;

        public PoeItemVerifier([NotNull] IFactory<IHttpClient> clientsFactory)
        {
            Guard.ArgumentNotNull(clientsFactory, nameof(clientsFactory));

            this.clientsFactory = clientsFactory;
        }

        public Task<bool?> Verify(IPoeItem item)
        {
            Guard.ArgumentNotNull(item, nameof(item));

            if (string.IsNullOrWhiteSpace(item.Hash) || string.IsNullOrWhiteSpace(item.ThreadId))
            {
                return Task.Run(() => default(bool?));
            }

            return Task.Run(() => VerifyInternal(item.Hash, item.ThreadId));
        }

        private Task<bool?> VerifyInternal(string hash, string threadId)
        {
            Guard.ArgumentNotNullOrEmpty(() => hash);
            Guard.ArgumentNotNullOrEmpty(() => threadId);

            var client = clientsFactory.Create();
            var verificationUri = $"{VerificationUri}/{threadId}/{hash}";

            Log.Debug($"[PoeItemVerifier] Requesting verification uri '{verificationUri}'...");
            return client
                   .Get(verificationUri)
                   .Select(response => new {response, isVerified = ExtractResult(response)})
                   .Do(result => PostProcess(verificationUri, result.response, result.isVerified))
                   .Select(x => x.isVerified)
                   .ToTask();
        }

        private void PostProcess(string verificationUri, string response, bool? isVerified)
        {
            if (isVerified == null)
            {
                Log.Debug($"[PoeItemVerifier] Failed to process response(uri '{verificationUri}'):\r\n\tResponse: '{response}'");
            }
        }

        private bool? ExtractResult(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
            {
                return null;
            }

            if (rawHtml.Equals("(true);", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rawHtml.Equals("(false);", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return null;
        }
    }
}