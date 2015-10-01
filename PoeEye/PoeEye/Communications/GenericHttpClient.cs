﻿namespace PoeEye.Communications
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Security.Policy;
    using System.Threading.Tasks;
    using System.Web;

    using CsQuery.ExtensionMethods.Internal;

    using DumpToText;

    using EasyHttp.Http;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Http;

    using TypeConverter;

    using HttpException = EasyHttp.Infrastructure.HttpException;
    using HttpResponse = EasyHttp.Http.HttpResponse;

    internal sealed class GenericHttpClient : IHttpClient
    {
        private readonly IConverter<NameValueCollection, string> nameValueConverter;

        public CookieCollection Cookies { get; set; }

        public GenericHttpClient(
                [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
        {
            Guard.ArgumentNotNull(() => nameValueConverter);
            
            this.nameValueConverter = nameValueConverter;
        }

        public IObservable<string> PostQuery(string uri, NameValueCollection args)
        {
            Guard.ArgumentNotNullOrEmpty(() => uri);
            Guard.ArgumentNotNull(() => args);

            return Observable.Start(() => PostQueryInternal(uri, args), Scheduler.Default);
        }

        private string PostQueryInternal(string uri, NameValueCollection args)
        {
            var httpClient = new HttpClient();
            httpClient.Request.Cookies = Cookies;

            var postData = nameValueConverter.Convert(args);
            Log.Instance.Debug($"[HttpClient] Querying uri '{uri}', args: \r\nPOST: {postData}\r\nSplitted: {postData.SplitClean('&').DumpToTextValue()}");
            
            var response = httpClient.Post(uri, postData, HttpContentTypes.ApplicationXWwwFormUrlEncoded);
            Log.Instance.Debug(
                $"[HttpClient] Received response, status: {response.StatusCode}, length: {response.RawText?.Length}");

            CheckResponseStatusOrThrow(response);

            return response.RawText;
        }

        private static void CheckResponseStatusOrThrow(HttpResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw new HttpException(response.StatusCode,
                $"Wrong status code, expected 200 OK, got {response.StatusCode}");
        }
    }
}