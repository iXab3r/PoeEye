using System.Reactive;
using System.Reactive.Concurrency;
using JetBrains.Annotations;
using PoeShared.Services;

namespace PoeShared.Scaffolding;

public static partial class Observables
{
    //There is a problem with FromAsync - by default, when an exception is thrown AFTER unsubscription, it gets propagated as unobserved exception thus crashing the domain
    //In most cases this does not make any sense as if subscription is no longer active - we're probably not very interested in exceptions as well
    
    /// <summary>
    /// Converts an asynchronous action into an observable sequence. Each subscription to the resulting sequence causes the action to be started.
    /// The CancellationToken passed to the asynchronous action is tied to the observable sequence's subscription that triggered the action's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <param name="actionAsync">Asynchronous action to convert.</param>
    /// <returns>An observable sequence exposing a Unit value upon completion of the action, or an exception.</returns>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actionAsync"/> is null.</exception>
    public static IObservable<T> FromAsyncSafe<T>(Func<Task<T>> actionAsync)
    {
        if (actionAsync == null)
        {
            throw new ArgumentNullException(nameof(actionAsync));
        }

        return Observable.FromAsync(actionAsync, new TaskObservationOptions(scheduler: null, ignoreExceptionsAfterUnsubscribe: true));
    }
    
    /// <summary>
    /// Converts an asynchronous action into an observable sequence. Each subscription to the resulting sequence causes the action to be started.
    /// The CancellationToken passed to the asynchronous action is tied to the observable sequence's subscription that triggered the action's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <param name="actionAsync">Asynchronous action to convert.</param>
    /// <param name="scheduler">Scheduler on which to notify observers.</param>
    /// <returns>An observable sequence exposing a Unit value upon completion of the action, or an exception.</returns>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actionAsync"/> is null.</exception>
    public static IObservable<T> FromAsyncSafe<T>(Func<Task<T>> actionAsync, IScheduler scheduler)
    {
        if (actionAsync == null)
        {
            throw new ArgumentNullException(nameof(actionAsync));
        }

        return Observable.FromAsync(actionAsync, new TaskObservationOptions(scheduler: scheduler, ignoreExceptionsAfterUnsubscribe: true));
    }
    
    /// <summary>
    /// Converts an asynchronous action into an observable sequence. Each subscription to the resulting sequence causes the action to be started.
    /// The CancellationToken passed to the asynchronous action is tied to the observable sequence's subscription that triggered the action's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <param name="actionAsync">Asynchronous action to convert.</param>
    /// <returns>An observable sequence exposing a Unit value upon completion of the action, or an exception.</returns>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actionAsync"/> is null.</exception>
    public static IObservable<Unit> FromAsyncSafe(Func<Task> actionAsync)
    {
        if (actionAsync == null)
        {
            throw new ArgumentNullException(nameof(actionAsync));
        }

        return Observable.FromAsync(actionAsync, new TaskObservationOptions(scheduler: null, ignoreExceptionsAfterUnsubscribe: true));
    }
    
    /// <summary>
    /// Converts an asynchronous action into an observable sequence. Each subscription to the resulting sequence causes the action to be started.
    /// The CancellationToken passed to the asynchronous action is tied to the observable sequence's subscription that triggered the action's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <param name="actionAsync">Asynchronous action to convert.</param>
    /// <param name="scheduler">Scheduler on which to notify observers.</param>
    /// <returns>An observable sequence exposing a Unit value upon completion of the action, or an exception.</returns>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actionAsync"/> is null.</exception>
    public static IObservable<Unit> FromAsyncSafe(Func<Task> actionAsync, IScheduler scheduler)
    {
        if (actionAsync == null)
        {
            throw new ArgumentNullException(nameof(actionAsync));
        }

        return Observable.FromAsync(actionAsync, new TaskObservationOptions(scheduler: scheduler, ignoreExceptionsAfterUnsubscribe: true));
    }
    
    /// <summary>
    /// Converts an asynchronous action into an observable sequence. Each subscription to the resulting sequence causes the action to be started.
    /// The CancellationToken passed to the asynchronous action is tied to the observable sequence's subscription that triggered the action's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <param name="actionAsync">Asynchronous action to convert.</param>
    /// <returns>An observable sequence exposing a Unit value upon completion of the action, or an exception.</returns>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actionAsync"/> is null.</exception>
    public static IObservable<Unit> FromAsyncSafe(Func<CancellationToken, Task> actionAsync)
    {
        if (actionAsync == null)
        {
            throw new ArgumentNullException(nameof(actionAsync));
        }

        return Observable.FromAsync(actionAsync, new TaskObservationOptions(scheduler: null, ignoreExceptionsAfterUnsubscribe: true));
    }

    /// <summary>
    /// Converts an asynchronous action into an observable sequence. Each subscription to the resulting sequence causes the action to be started.
    /// The CancellationToken passed to the asynchronous action is tied to the observable sequence's subscription that triggered the action's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <param name="actionAsync">Asynchronous action to convert.</param>
    /// <param name="scheduler">Scheduler on which to notify observers.</param>
    /// <returns>An observable sequence exposing a Unit value upon completion of the action, or an exception.</returns>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actionAsync"/> is null.</exception>
    public static IObservable<Unit> FromAsyncSafe(Func<CancellationToken, Task> actionAsync, IScheduler scheduler)
    {
        if (actionAsync == null)
        {
            throw new ArgumentNullException(nameof(actionAsync));
        }

        if (scheduler == null)
        {
            throw new ArgumentNullException(nameof(scheduler));
        }

        return Observable.FromAsync(actionAsync, new TaskObservationOptions(scheduler: scheduler, ignoreExceptionsAfterUnsubscribe: true));
    }
    
    /// <summary>
    /// Converts an asynchronous function into an observable sequence. Each subscription to the resulting sequence causes the function to be started.
    /// The CancellationToken passed to the asynchronous function is tied to the observable sequence's subscription that triggered the function's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the asynchronous function.</typeparam>
    /// <param name="functionAsync">Asynchronous function to convert.</param>
    /// <returns>An observable sequence exposing the result of invoking the function, or an exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functionAsync"/> is null.</exception>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    public static IObservable<TResult> FromAsyncSafe<TResult>(Func<CancellationToken, Task<TResult>> functionAsync)
    {
        if (functionAsync == null)
        {
            throw new ArgumentNullException(nameof(functionAsync));
        }

        return Observable.FromAsync(functionAsync, new TaskObservationOptions(scheduler: null, ignoreExceptionsAfterUnsubscribe: true));
    }
    
    /// <summary>
    /// Converts an asynchronous function into an observable sequence. Each subscription to the resulting sequence causes the function to be started.
    /// The CancellationToken passed to the asynchronous function is tied to the observable sequence's subscription that triggered the function's invocation and can be used for best-effort cancellation.
    /// Sets ignoreExceptionsAfterUnsubscribe to true to ignore post-unsub exceptions which tend to be propagated to app domain and crash the app
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the asynchronous function.</typeparam>
    /// <param name="functionAsync">Asynchronous function to convert.</param>
    /// <param name="scheduler">Scheduler on which to notify observers.</param>
    /// <returns>An observable sequence exposing the result of invoking the function, or an exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functionAsync"/> is null.</exception>
    /// <remarks>When a subscription to the resulting sequence is disposed, the CancellationToken that was fed to the asynchronous function will be signaled.</remarks>
    public static IObservable<TResult> FromAsyncSafe<TResult>(Func<CancellationToken, Task<TResult>> functionAsync, IScheduler scheduler)
    {
        if (functionAsync == null)
        {
            throw new ArgumentNullException(nameof(functionAsync));
        }

        return Observable.FromAsync(functionAsync, new TaskObservationOptions(scheduler: scheduler, ignoreExceptionsAfterUnsubscribe: true));
    }
}