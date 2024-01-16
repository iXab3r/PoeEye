using System;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using PoeShared.Blazor.Internals;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Blazor;

/// <summary>
/// Represents the base class for reactive components in a Blazor application.
/// </summary>
public abstract class BlazorReactiveComponentBase : ReactiveComponentBase
{
    private static readonly Binder<BlazorReactiveComponentBase> Binder = new();

    private readonly Subject<object> whenChanged = new();
    private readonly ReactiveChangeDetector changeDetector = new();
    
    /// <summary>
    /// Static constructor to initialize the binder for the component.
    /// </summary>
    static BlazorReactiveComponentBase()
    {
        Binder.BindIf(x => !(x.JsRuntime is SafeJsRuntime), x => x.JsRuntime == null ? default(IJSRuntime) : new SafeJsRuntime(x.JsRuntime, x))
            .To(x => x.JsRuntime);
    }

    /// <summary>
    /// Constructor to initialize the BlazorReactiveComponentBase instance.
    /// </summary>
    protected BlazorReactiveComponentBase()
    {
        this.WhenAnyProperty(x => x.DataContext)
            .Subscribe(x => whenChanged.OnNext("DataContext has changed"))
            .AddTo(Anchors);
        changeDetector.WhenChanged
            .Subscribe(x => whenChanged.OnNext(x)).AddTo(Anchors);
        WhenChanged.Subscribe(WhenRefresh).AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
    }
    
    /// <summary>
    /// Gets or sets the JavaScript runtime associated with the component.
    /// </summary>
    [Inject] public IJSRuntime JsRuntime { get; private set; }
    
    /// <summary>
    /// Gets or sets the data context for the component.
    /// </summary>
    [Parameter] public object DataContext { get; set; }
    
    /// <summary>
    /// Gets or sets user-defined class names, separated by space.
    /// </summary>
    [Parameter]
    public string Class { get; set; }

    /// <summary>
    /// Gets or sets user-defined Id, not all components may be using it
    /// </summary>
    [Parameter]
    public string Id { get; set; }
    
    /// <summary>
    /// Gets or sets user-defined styles, applied on top of the component's own classes and styles.
    /// </summary>
    [Parameter]
    public string Style { get; set; }
    
    /// <summary>
    /// Observable sequence that signals when a property of the component changes.
    /// </summary>
    public IObservable<object> WhenChanged => whenChanged;
    
    /// <summary>
    /// Tracks changes in the specified context using a selector expression.
    /// </summary>
    /// <typeparam name="TExpressionContext">The type of the context to track changes in.</typeparam>
    /// <typeparam name="TOut">The type of the output from the selector expression.</typeparam>
    /// <param name="context">The context to track changes in.</param>
    /// <param name="selector">The expression used to select the property to track.</param>
    /// <returns>The value of the property selected by the provided expression.</returns>
    public TOut Track<TExpressionContext, TOut>(TExpressionContext context, Expression<Func<TExpressionContext, TOut>> selector) where TExpressionContext : class
    {
        try
        {
            return changeDetector.Track(context, selector);
        }
        catch (Exception e)
        {
            throw new InvalidStateException($"Failed to initialize change tracking in component {this} ({GetType()}) (data context: {DataContext}) for expression: {selector}{(ReferenceEquals(context, DataContext) ? "" : $", expression context: {context}")}", e);
        }
    }
    
    /// <summary>
    /// Represents a safe wrapper around the IJSRuntime to ensure proper usage within the component.
    /// </summary>
    private sealed class SafeJsRuntime : IJSRuntime
    {
        private readonly IJSRuntime jsRuntime;
        private readonly BlazorReactiveComponentBase owner;

        public SafeJsRuntime(IJSRuntime jsRuntime, BlazorReactiveComponentBase owner)
        {
            this.jsRuntime = jsRuntime;
            this.owner = owner;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            if (owner.Anchors.IsDisposed)
            {
                return default;
            }
            try
            {
                return await jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
            }
            catch (Exception e)
            {
                owner.Log.Warn($"Component has encountered JS invocation error, identifier: {identifier}", e);
                owner.Dispose();

                await owner.InvokeAsync(() => throw new AggregateException(
                    new InvalidOperationException("Do not forget to await JS invocations!"),
                    e));
                return default;
            }
        }
    }
}
