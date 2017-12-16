using System;
using System.Collections.Specialized;
using System.Net;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using CsQuery.ExtensionMethods.Internal;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using RestSharp;
using TypeConverter;
using Cookie = System.Net.Cookie;

namespace PoeShared.Communications.Chromium {
    internal sealed class ChromiumBrowser : DisposableReactiveObject, IPoeBrowser 
    {
        private readonly ChromiumWebBrowser instance;

        private readonly Lazy<string> browserId;
        
        [NotNull] private readonly IConverter<NameValueCollection, string> nameValueConverter;
        public ChromiumBrowser(
            ChromiumWebBrowser instance,
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(nameValueConverter, nameof(nameValueConverter));
            
            browserId = new Lazy<string>(() => $"#{instance.GetBrowser().Identifier}");

            this.instance = instance;
            this.nameValueConverter = nameValueConverter;
            instance.AddTo(Anchors);
            
            Log.Instance.Debug($"[Chromium{Id}] Initialized");
            
            instance.LoadError += InstanceOnLoadError;
            instance.ConsoleMessage += InstanceOnConsoleMessage;
            
            Disposable.Create(
                () =>
                {
                    Log.Instance.Debug($"[Chromium{Id}] Disposed @ URI {Address}");
                }).AddTo(Anchors);
        }
        
        private void InstanceOnConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            Log.Instance.Debug($"[Chromium{Id} Console] Line #{args.Line}, Source: {args.Source}, Message: {args.Message}");
        }

        public string Address => instance.Address;

        public string Id => browserId.Value;
        
        public Task Get(string uri)
        {
            return Post(uri, new NameValueCollection());
            var frame = instance.GetMainFrame();
            var request = frame.CreateRequest(false);
            request.Url = uri;
            request.Method = WebRequestMethods.Http.Get;
            //request.InitializePostData();

            //var headers = new NameValueCollection();
            //request.Headers = headers;
                
            return Load(frame, request);
        }
        
        public Task Post(string uri, NameValueCollection args)
        {
            var postData = nameValueConverter.Convert(args);
            Log.Instance.Debug($"[Chromium{Id}] Querying uri '{uri}', args: \r\nPOST: {postData}");
            Log.Instance.Trace($"[Chromium{Id}] Splitted POST data dump: {postData.SplitClean('&').DumpToText()}");

            using (var frame = instance.GetMainFrame())
            {
                var request = frame.CreateRequest(initializePostData: true);
                request.Url = uri;
                request.Method = WebRequestMethods.Http.Post;
            
                Log.Instance.Debug($"[Chromium{Id}] Preparing POST data...");
                if (args.Count > 0)
                {
                    request.PostData.AddData(postData, Encoding.ASCII);
                }
                
                var headers = new NameValueCollection();
                headers.Add("Content-Type", "application/x-www-form-urlencoded" );
                request.Headers = headers;

                return Load(frame, request);
            }
        }

        public Task<string> GetSource()
        {
            var result = instance.GetSourceAsync();
            return result;
        }

        private Task Load(IFrame frame, IRequest request)
        {
            Log.Instance.Debug(
                $"[Chromium{Id}] Loading request of type {request.Method}, URI: {request.Url}");

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
                Log.Instance.Debug(
                    $"[Chromium{Id}.LoadingState] Loading state changed, isLoading: {args.IsLoading}, uri: {args.Browser?.MainFrame?.Url}");

                if (args.IsLoading)
                {
                    return;
                }
                
                Log.Instance.Debug(
                    $"[Chromium{Id}.LoadingState] Received response, address: {instance.Address}");
            }

            void StatusMessageEventHandler(object sender, StatusMessageEventArgs args)
            {
                Log.Instance.Debug(
                    $"[Chromium{Id}.Status] Status message: '{args.Value}', uri: {args.Browser?.MainFrame?.Url}, isLoading: {args.Browser?.IsLoading}");
            }
            
            void InstanceOnFrameLoadStart(object o, FrameLoadStartEventArgs args)
            {
                if (args.Frame.Identifier != args.Browser.MainFrame.Identifier)
                {
                    Log.Instance.Debug(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadStart] TransitionType: {args.TransitionType}, uri: {args.Url}");
                }
                else
                {
                    Log.Instance.Debug(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadStart] MainFrame loaded, TransitionType: {args.TransitionType}, uri: {args.Url}");
                    
                    
                    Unsubscribe();
                    taskCompetionSource.TrySetResult(true);
                }
            }
            
            void InstanceOnFrameLoadEnd(object o, FrameLoadEndEventArgs args)
            {
                if (args.Frame.Identifier != args.Browser.MainFrame.Identifier)
                {
                    Log.Instance.Debug(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadEnd] StatusCode: {args.HttpStatusCode}, uri: {args.Url}");
                }
                else
                {
                    Log.Instance.Debug(
                        $"[Chromium{Id}.Frame#{args.Frame.Identifier} LoadEnd] MainFrame loaded, StatusCode: {args.HttpStatusCode}, uri: {args.Url}");
                    
                    
                    Unsubscribe();
                    taskCompetionSource.TrySetResult(true);
                }
            }
            
            Subscribe();
            frame.LoadRequest(request);

            return taskCompetionSource.Task;
        }
        
        private void InstanceOnLoadError(object sender1, LoadErrorEventArgs args)
        {
            Log.Instance.Warn(
                $"[Chromium{Id}.Frame#{args.Frame.Identifier} ] Failed uri: {args.FailedUrl}, error: {args.ErrorText}, errorCode: {args.ErrorCode}");
        }
    }
}