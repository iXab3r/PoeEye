using System.Security.Cryptography.X509Certificates;
using CefSharp;

namespace PoeShared.Communications.Chromium
{
    internal sealed class LogRequestHandler : IRequestHandler
    {
        public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnBeforeBrowse executed, request: {request.Url}");
            return false;
        }

        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition,
                                     bool userGesture)
        {
            Log.Instance.Trace(
                $"[Chromium{GetBrowserId(browserControl)}] OnOpenUrlFromTab executed, targetUrl: {targetUrl}, targetDisposition: {targetDisposition}");
            return false;
        }

        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo,
                                       IRequestCallback callback)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnCertificateError executed");
            return false;
        }

        public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnPluginCrashed executed, pluginPath: {pluginPath}");
        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnBeforeResourceLoad executed, request: {request.Method} '{request.Url}'");
            // Return true to continue the request and call CefAuthCallback::Continue() when the authentication information is available. Return false to cancel the request.
            return CefReturnValue.Continue;
        }

        public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm,
                                       string scheme, IAuthCallback callback)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] GetAuthCredentials executed");
            //Return true to continue the request and call CefAuthCallback::Continue() when the authentication information is available. Return false to cancel the request.
            return false;
        }

        public bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port,
                                              X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnSelectClientCertificate executed");
            //Return true to continue the request and call ISelectClientCertificateCallback.Select() with the selected certificate for authentication.
            // Return false to use the default behavior where the browser selects the first certificate from the list.
            return false;
        }

        public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnRenderProcessTerminated executed, status: {status}");
        }

        public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnQuotaRequest executed, originUrl: {originUrl}, newSize: {newSize}");
            //Return false to cancel the request immediately. Return true to continue the request
            // and call <see cref="M:CefSharp.IRequestCallback.Continue(System.Boolean)" /> either in this method or at a later time to
            // grant or deny the request.
            return false;
        }

        public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnResourceRedirect executed, request: {request.Url}, newUrl: {newUrl}");
        }

        public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnProtocolExecution executed, url: {url}");
            //return to true to attempt execution via the registered OS protocol handler, if any. Otherwise return false.
            return true;
        }

        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnRenderViewReady executed");
        }

        public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnResourceResponse executed, request: {request.Method} '{request.Url}'");
            //To allow the resource to load normally return false.
            // To redirect or retry the resource modify request (url, headers or post body) and return true.
            return false;
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] GetResourceResponseFilter executed");
            //Return an IResponseFilter to intercept this response, otherwise return null
            return null;
        }

        public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response,
                                           UrlRequestStatus status, long receivedContentLength)
        {
            Log.Instance.Trace($"[Chromium{GetBrowserId(browserControl)}] OnResourceLoadComplete executed, {request.Method} '{request.Url}'");
        }

        private string GetBrowserId(IWebBrowser browserControl)
        {
            if (browserControl.IsBrowserInitialized)
            {
                return $"#{browserControl.GetBrowser().Identifier}";
            }

            return "#Unknown";
        }
    }
}