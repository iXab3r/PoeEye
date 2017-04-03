using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using Guards;
using JetBrains.Annotations;
using PoeShared;

namespace PoeOracle.Models
{
    public class ExternalUriOpener : IExternalUriOpener
    {
        private readonly ISubject<Unit> requested = new Subject<Unit>();

        public IObservable<Unit> Requested => requested;

        public void Request(string uri)
        {
            Guard.ArgumentNotNull(uri, nameof(uri));
            try
            {
                requested.OnNext(Unit.Default);

                Process.Start(uri);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[ExternalUriOpener] Failed to open Uri '{uri}'", ex);
            }
        }
    }
}
