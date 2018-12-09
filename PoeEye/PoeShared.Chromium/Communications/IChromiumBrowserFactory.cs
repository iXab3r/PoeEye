using System;
using JetBrains.Annotations;

namespace PoeShared.Chromium.Communications
{
    public interface IChromiumBrowserFactory : IDisposable
    {
        [NotNull]
        IChromiumBrowser CreateBrowser();
    }
}