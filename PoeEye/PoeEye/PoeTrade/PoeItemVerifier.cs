namespace PoeEye.PoeTrade
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.Http;

    internal sealed class PoeItemVerifier : IPoeItemVerifier
    {
        private readonly IFactory<IHttpClient> clientsFactory;
        private static readonly string VerificationUri = "http://verify.xyz.is/";

        public PoeItemVerifier([NotNull] IFactory<IHttpClient> clientsFactory)
        {
            Guard.ArgumentNotNull(() => clientsFactory);
            
            this.clientsFactory = clientsFactory;
        }

        public Task<bool?> Verify(IPoeItem item)
        {
            Guard.ArgumentNotNull(() => item);

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

            Log.Instance.Debug($"[PoeItemVerifier] Requesting verification uri '{verificationUri}'...");
            return client
                .Get(verificationUri)
                .Select(response => new { response, isVerified = ExtractResult(response) })
                .Do(result => PostProcess(verificationUri, result.response, result.isVerified))
                .Select(x => x.isVerified)
                .ToTask();
        }

        private void PostProcess(string verificationUri, string response, bool? isVerified)
        {
            if (isVerified == null)
            {
                Log.Instance.Debug($"[PoeItemVerifier] Failed to process response(uri '{verificationUri}'):\r\n\tResponse: '{response}'");
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
            else if (rawHtml.Equals("(false);", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }
}