using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Internals;
using CefSharp.OffScreen;
using Common.Logging;
using CsQuery.ExtensionMethods.Internal;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeShared.Communications.Chromium
{
    internal sealed class ChromiumBrowser : DisposableReactiveObject, IChromiumBrowser
    {
        private static readonly ILog Log = LogManager.GetLogger<ChromiumBrowser>();
        
        private readonly Lazy<string> browserId;
        private readonly ChromiumWebBrowser instance;

        private readonly IConverter<NameValueCollection, string> nameValueConverter;
        private readonly IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>> nameValueToListConverter;

        public ChromiumBrowser(
            ChromiumWebBrowser instance,
            [NotNull] IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>> nameValueToListConverter,
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(nameValueToListConverter, nameof(nameValueToListConverter));
            Guard.ArgumentNotNull(nameValueConverter, nameof(nameValueConverter));

            browserId = new Lazy<string>(() => $"#{instance.GetBrowser().Identifier}");

            this.instance = instance;
            this.nameValueToListConverter = nameValueToListConverter;
            this.nameValueConverter = nameValueConverter;
            instance.AddTo(Anchors);

            Log.Debug($"[Chromium{Id}] Initialized");

            instance.LoadError += InstanceOnLoadError;
            instance.ConsoleMessage += InstanceOnConsoleMessage;

            Disposable.Create(
                () => { Log.Debug($"[Chromium{Id}] Disposed @ URI {Address}"); }).AddTo(Anchors);
        }

        public string Id => browserId.Value;

        public string Address => instance.Address;

        public Task Get(string uri)
        {
            return GetUsingJavascript(uri, WebRequestMethods.Http.Get, new NameValueCollection());
        }

        public Task Post(string uri, NameValueCollection args)
        {
            return GetUsingJavascript(uri, WebRequestMethods.Http.Post, args);
        }

        public Task<string> GetSource()
        {
            var result = instance.GetSourceAsync();
            return result;
        }

        private void InstanceOnConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            Log.Trace($"[Chromium{Id} Console] Line #{args.Line}, Source: {args.Source}, Message: {args.Message}");
        }

        private Task GetUsingJavascript(string uri, string method, NameValueCollection elements)
        {
            var blankPage = "<html><script language='javascript'>var init = 1</script><body><h1>Hello world!</h1></body></html>";
            Log.Debug($"[Chromium{Id}] Loading bootstrapper page");

            Load(() => instance.LoadHtml(blankPage)).Wait();

            var js = new StringBuilder();

            var formBuilder = new StringBuilder();
            formBuilder.Append($"<form id=\"dynForm\" action=\"{uri}\" method=\"{method}\"> ");
            if (elements.Count > 0)
            {
                foreach (var element in nameValueToListConverter.Convert(elements))
                {
                    formBuilder.Append($"<input type=\"hidden\" name=\"{element.Key}\" value=\"{element.Value}\"> \\\\");
                }
            }

            formBuilder.Append($" </form>");

            js.AppendLine($"document.body.innerHTML = '{formBuilder}';");
            js.AppendLine($"document.getElementById(\"dynForm\").submit();");

            Log.Trace($"[Chromium{Id}] Executing JS\n{js}");

            var executeScript = Load(() => instance.ExecuteScriptAsync(js.ToString()));
            return executeScript.ContinueWith(task => DumpSource());
        }

        private Task DumpSource()
        {
            return instance.GetSourceAsync()
                           .ContinueWith(source => Log.Trace($"[Chromium{Id}] Source @ {instance.Address}: \n{source.Result}"));
        }

        private Task GetInternal(string uri)
        {
            using (var frame = instance.GetMainFrame())
            {
                var request = frame.CreateRequest(false);
                request.Url = uri;
                request.Method = WebRequestMethods.Http.Get;

                return Load(request, req => frame.LoadRequest(req));
            }
        }

        public Task PostInternal(string uri, NameValueCollection args)
        {
            var postData = nameValueConverter.Convert(args);
            Log.Debug($"[Chromium{Id}] Querying uri '{uri}', args: \r\nPOST: {postData}");
            Log.Trace($"[Chromium{Id}] Splitted POST data dump: {postData.SplitClean('&').DumpToText()}");

            using (var frame = instance.GetMainFrame())
            {
                var request = frame.CreateRequest(true);
                request.Url = uri;
                request.Method = WebRequestMethods.Http.Post;

                Log.Debug($"[Chromium{Id}] Preparing POST data...");
                if (args.Count > 0)
                {
                    request.PostData.AddData(postData, Encoding.ASCII);
                }

                var headers = new NameValueCollection();
                headers.Add("Content-Type", "application/x-www-form-urlencoded");
                request.Headers = headers;

                return Load(request, x => frame.LoadRequest(x));
            }
        }

        private Task Load(IRequest request, Action<IRequest> loader)
        {
            Log.Debug($"[Chromium{Id}] Loading request of type {request.Method}, URI: {request.Url}");

            return Load(() => loader(request));
        }

        private Task Load(Action loader)
        {
            var taskCompetionSource = new TaskCompletionSource<bool>();

            void Unsubscribe()
            {
                instance.FrameLoadStart -= InstanceOnFrameLoadStart;
                instance.FrameLoadEnd -= InstanceOnFrameLoadEnd;
                instance.LoadingStateChanged -= LoadingStateChangedHandler;
                instance.StatusMessage -= StatusMessageEventHandler;
            }

            void Subscribe()
            {
                instance.FrameLoadStart += InstanceOnFrameLoadStart;
                instance.FrameLoadEnd += InstanceOnFrameLoadEnd;
                instance.LoadingStateChanged += LoadingStateChangedHandler;
                instance.StatusMessage += StatusMessageEventHandler;
            }

            void LoadingStateChangedHandler(object sender, LoadingStateChangedEventArgs args)
            {
                Log.Debug(
                    $"[Chromium{Id}.LoadingState] Loading state changed, isLoading: {args.IsLoading}, uri: {args.Browser?.MainFrame?.Url}");

                if (args.IsLoading)
                {
                    return;
                }

                Log.Debug(
                    $"[Chromium{Id}.LoadingState] Received response, address: {instance.Address}");
            }

            void StatusMessageEventHandler(object sender, StatusMessageEventArgs args)
            {
                Log.Debug(
                    $"[Chromium{Id}.Status] Status message: '{args.Value}', uri: {args.Browser?.MainFrame?.Url}, isLoading: {args.Browser?.IsLoading}");
            }

            void InstanceOnFrameLoadStart(object o, FrameLoadStartEventArgs args)
            {
                if (args.Frame.Identifier != args.Browser.MainFrame.Identifier)
                {
                    Log.Trace(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadStart] TransitionType: {args.TransitionType}, uri: {args.Url}");
                }
                else
                {
                    Log.Trace(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadStart] MainFrame loaded, TransitionType: {args.TransitionType}, uri: {args.Url}");
                }
            }

            void InstanceOnFrameLoadEnd(object o, FrameLoadEndEventArgs args)
            {
                if (args.Frame.Identifier != args.Browser.MainFrame.Identifier)
                {
                    Log.Trace(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadEnd] StatusCode: {args.HttpStatusCode}, uri: {args.Url}");
                }
                else
                {
                    Log.Trace(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadEnd] MainFrame loaded, StatusCode: {args.HttpStatusCode}, uri: {args.Url}");


                    Unsubscribe();
                    taskCompetionSource.TrySetResultAsync(true);
                }
            }

            Subscribe();
            loader();

            Log.Debug($"[Chromium{Id}] Returning task");

            return taskCompetionSource.Task;
        }

        private void InstanceOnLoadError(object sender1, LoadErrorEventArgs args)
        {
            Log.Warn(
                $"[Chromium{Id}.Frame#{args.Frame.Identifier} ] Failed uri: {args.FailedUrl}, error: {args.ErrorText}, errorCode: {args.ErrorCode}");
        }
    }
}