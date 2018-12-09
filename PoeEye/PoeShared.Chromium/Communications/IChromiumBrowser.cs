using System.Collections.Specialized;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Chromium.Communications
{
    public interface IChromiumBrowser : IDisposableReactiveObject
    {
        string Address { [CanBeNull] get; }

        Task Get([NotNull] string uri);

        [NotNull]
        Task<string> GetSource();

        [NotNull]
        Task Post(string uri, NameValueCollection args);
    }
}