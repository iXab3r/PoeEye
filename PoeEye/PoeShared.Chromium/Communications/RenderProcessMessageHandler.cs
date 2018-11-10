using CefSharp;
using Common.Logging;

namespace PoeShared.Chromium.Communications
{
    internal sealed class RenderProcessMessageHandler : IRenderProcessMessageHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RenderProcessMessageHandler));
        
        // Wait for the underlying JavaScript Context to be created. This is only called for the main frame.
        // If the page has no JavaScript, no context will be created.
        void IRenderProcessMessageHandler.OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            Log.Trace($"[Chromium#{browserControl.GetBrowser().Identifier} Frame#{frame.Identifier}] OnContextCreated executed");
        }

        public void OnContextReleased(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            Log.Trace($"[Chromium#{browserControl.GetBrowser().Identifier} Frame#{frame.Identifier}] OnContextReleased executed");
        }

        public void OnFocusedNodeChanged(IWebBrowser browserControl, IBrowser browser, IFrame frame, IDomNode node)
        {
            Log.Trace($"[Chromium#{browserControl.GetBrowser().Identifier} Frame#{frame.Identifier}] OnFocusedNodeChanged executed");
        }
    }
}