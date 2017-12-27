using CefSharp;

namespace PoeShared.Communications.Chromium {
    internal sealed class RenderProcessMessageHandler : IRenderProcessMessageHandler
    {
        // Wait for the underlying JavaScript Context to be created. This is only called for the main frame.
        // If the page has no JavaScript, no context will be created.
        void IRenderProcessMessageHandler.OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            Log.Instance.Trace($"[Chromium#{browserControl.GetBrowser().Identifier} Frame#{frame.Identifier}] OnContextCreated executed");
        }
        
        public void OnContextReleased(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            Log.Instance.Trace($"[Chromium#{browserControl.GetBrowser().Identifier} Frame#{frame.Identifier}] OnContextReleased executed");
        }
        
        public void OnFocusedNodeChanged(IWebBrowser browserControl, IBrowser browser, IFrame frame, IDomNode node)
        {
            Log.Instance.Trace($"[Chromium#{browserControl.GetBrowser().Identifier} Frame#{frame.Identifier}] OnFocusedNodeChanged executed");
        }
    }
}