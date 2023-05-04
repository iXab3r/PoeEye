using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Blazor;

public abstract class BlazorReactiveComponentBase : ReactiveComponentBase
{
    private static readonly Binder<BlazorReactiveComponentBase> Binder = new();

    protected static readonly ConcurrentDictionary<Type, PropertyInfo[]> CollectionProperties = new();

    static BlazorReactiveComponentBase()
    {
        Binder.BindIf(x => !(x.JsRuntime is SafeJsRuntime), x => x.JsRuntime == null ? default(IJSRuntime) : new SafeJsRuntime(x.JsRuntime, x))
            .To(x => x.JsRuntime);
    }

    protected BlazorReactiveComponentBase()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    [Inject] public IJSRuntime JsRuntime { get; private set; }
    
    [Parameter] public object DataContext { get; set; }

    private sealed class SafeJsRuntime : IJSRuntime
    {
        public SafeJsRuntime(IJSRuntime jsRuntime, BlazorReactiveComponentBase owner)
        {
            JsRuntime = jsRuntime;
            Owner = owner;
        }

        public IJSRuntime JsRuntime { get; }

        public BlazorReactiveComponentBase Owner { get; }

        /// <inheritdoc />
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        /// <inheritdoc />
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            if (Owner.Anchors.IsDisposed)
            {
                return default;
            }
            try
            {
                return await JsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
            }
            catch (Exception e)
            {
                Owner.Log.Warn($"Component has encountered JS invocation error, identifier: {identifier}", e);
                Owner.Dispose();

                await Owner.InvokeAsync(() => throw new AggregateException(
                    new InvalidOperationException("Do not forget to await JS invocations!"),
                    e));
                return default;
            }
        }
    }
}