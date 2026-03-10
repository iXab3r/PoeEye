using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.WinForms;

internal sealed class BlazorWinFormsDispatcherProvider : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(BlazorWinFormsDispatcherProvider).PrepareLogger();
    private static readonly Lazy<BlazorWinFormsDispatcherProvider> InstanceSupplier = new();
    private readonly ConcurrentDictionary<string, Dispatcher> dispatchers = new();

    public static BlazorWinFormsDispatcherProvider Instance => InstanceSupplier.Value;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    public Dispatcher GetOrAdd(string name)
    {
        Log.Debug($"Retrieving dispatcher {name}");
        return dispatchers.GetOrAdd(name, CreateDispatcher);
    }

    private static Dispatcher CreateDispatcher(string name)
    {
        var consumer = new TaskCompletionSource<Dispatcher>();
        var thread = new Thread(RunThread)
        {
            Name = $"BWF#{name}",
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(consumer);
        return consumer.Task.Result;
    }

    private static void RunThread(object arg)
    {
        var consumer = (TaskCompletionSource<Dispatcher>)arg;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var dispatcher = Dispatcher.CurrentDispatcher;
            consumer.TrySetResult(dispatcher);
            Application.Run();
        }
        catch (Exception e)
        {
            consumer.TrySetException(e);
            Log.HandleException(e);
            throw;
        }
    }
}
