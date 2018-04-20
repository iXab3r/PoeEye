using System;
using JetBrains.Annotations;

namespace PoeShared.Communications.Chromium {
    internal interface IChromiumBrowserFactory : IDisposable
    {
        [NotNull] 
        IChromiumBrowser CreateBrowser();
    }
}