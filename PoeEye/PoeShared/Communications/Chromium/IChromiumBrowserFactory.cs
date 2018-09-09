using System;
using JetBrains.Annotations;

namespace PoeShared.Communications.Chromium
{
    public interface IChromiumBrowserFactory : IDisposable
    {
        [NotNull]
        IChromiumBrowser CreateBrowser();
    }
}