#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Core;
using ReactiveUI;

namespace PoeShared.Squirrel.Updater;

/// <summary>
/// Owns generic mutations, read-only notes lifetimes and native/registered handler bindings for the app.
/// A removed author binding never becomes a native NUPKG source in this lifetime.
/// </summary>
internal sealed class ApplicationUpdateHandlerRegistrator : DisposableReactiveObject, IApplicationUpdateHandlerRegistrator
{
    private static readonly IFluentLog Log = typeof(ApplicationUpdateHandlerRegistrator).PrepareLogger();
    private readonly object gate = new();
    private readonly Dictionary<Uri, Binding?> bindings = new();
    private readonly UpdateSourceProviderFromConfig sources;
    private readonly Binding nativeBinding;
    private State state;
    private Operation? active;
    private int nativeOperationsStarting;
    private readonly List<Task> nativeOperations = new();
    private readonly List<NotesRequest> notes = new();
    private NotesIdentity? notesIdentity;

    public ApplicationUpdateHandlerRegistrator(UpdateSourceProviderFromConfig sources, IApplicationUpdaterModel model,
        IAppArguments app, IFactory<INativeApplicationUpdateChangelogProvider> changelogs,
        ApplicationUpdateInstallationService installation)
    {
        this.sources = sources;
        nativeBinding = new Binding(new NativeApplicationUpdateHandler(model, app, changelogs, installation));
        state = EmptyState(sources.Selection, null);
        // Classify the actual model edge before any UI scheduling. A later legacy operation
        // retires completed generic progress even if it downloads the same native result.
        model.WhenAnyValue(x => x.IsBusy).Where(x => x)
            .Subscribe(_ => RetireCompletedNativeProgress()).AddTo(Anchors);
        sources.SelectionChanged += SelectionChanged;
        Disposable.Create(() => sources.SelectionChanged -= SelectionChanged).AddTo(Anchors);
    }

    internal event Action? Changed;
    internal event Action? HandedOff;

    internal bool IsBusy { get { lock (gate) return IsBusyLocked(); } }

    /// <summary>
    /// Keeps registered mutations Busy through a native compatibility call's actual task,
    /// preserving that task's identity and lifetime independently of UI notifications.
    /// An optional mutation refusal returns its Task without starting native work.
    /// </summary>
    internal TTask TrackNativeOperation<TTask>(Func<TTask> startOperation, Func<TTask>? onMutationBusy = null)
        where TTask : Task
    {
        bool mutationBusy;
        lock (gate)
        {
            mutationBusy = onMutationBusy != null && active != null && (active.IsApply || active.IsDownload);
            if (!mutationBusy) nativeOperationsStarting++;
        }
        if (mutationBusy) return onMutationBusy!();
        TTask task;
        try { task = startOperation(); }
        catch
        {
            lock (gate) nativeOperationsStarting--;
            throw;
        }
        lock (gate)
        {
            nativeOperations.Add(task);
            nativeOperationsStarting--;
        }
        task.ContinueWith(_ =>
        {
            lock (gate) nativeOperations.Remove(task);
            Changed?.Invoke();
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).AndForget();
        return task;
    }

    private bool IsBusyLocked() => active != null || nativeOperationsStarting != 0 || nativeOperations.Any(x => !x.IsCompleted);
    internal IApplicationUpdateInfo? SelectedUpdate => ReadState().Target;
    internal IPoeUpdateInfo? NativeUpdate => ReadState().NativeUpdate;
    internal int? NativeProgressPercent
    {
        get
        {
            lock (gate)
            {
                var selection = sources.Selection;
                if (!ReferenceEquals(state.Selection, selection) || !ReferenceEquals(state.Binding, nativeBinding)) return null;
                var running = active != null && ReferenceEquals(active.Selection, selection) && ReferenceEquals(active.Binding, nativeBinding);
                return running || state.Phase == ApplicationUpdatePhase.ReadyToApply && state.NativeProgressVisible
                    ? (int)state.Progress.Percent : null;
            }
        }
    }

    internal void RetireCompletedNativeProgress()
    {
        lock (gate)
        {
            if (active != null && ReferenceEquals(active.Binding, nativeBinding)) return;
            if (!state.NativeProgressVisible) return;
            state = state with { NativeProgressVisible = false };
        }
        Changed?.Invoke();
    }
    internal string InstalledVersionText => ReadState().Installed;
    internal string TargetVersionText => ReadState().Version;
    internal ApplicationUpdatePhase Phase => ReadState().Phase;
    internal bool CanApplyPreparedUpdate => ReadState() is { Target: not null, Phase: ApplicationUpdatePhase.ReadyToApply };
    internal string ProgressStage => ReadState().Progress.Stage;
    internal int ProgressPercent => (int)ReadState().Progress.Percent;
    internal bool IsProgressIndeterminate => ReadState().Progress.Indeterminate;
    internal bool IsInErrorStatus => ReadState().Error != null;
    internal string StatusText
    {
        get
        {
            var current = ReadState();
            return current.Error ?? (current.Phase == ApplicationUpdatePhase.Checking ? "Checking for updates..." : string.Empty);
        }
    }

    internal bool RoutesCurrentSource
    {
        get { lock (gate) return IsAuthorSource(sources.Selection); }
    }

    internal bool UsesGenericProjection
    {
        get
        {
            lock (gate)
            {
                var selection = sources.Selection;
                return active?.Accepted == true || selection.Source == null || IsAuthorSource(selection);
            }
        }
    }

    public IDisposable Register(Uri uri, IApplicationUpdateHandler handler)
    {
        Guard.ArgumentNotNull(uri, nameof(uri));
        Guard.ArgumentNotNull(handler, nameof(handler));
        var binding = new Binding(handler);
        List<Operation> invalidate;
        lock (gate)
        {
            bindings[uri] = binding;
            invalidate = RefreshSelection();
        }
        Invalidate(invalidate);
        Changed?.Invoke();
        return Disposable.Create(() =>
        {
            List<Operation> removedOperation;
            lock (gate)
            {
                if (!bindings.TryGetValue(uri, out var current) || !ReferenceEquals(current, binding)) return;
                // Retain the URI's author classification while removing the actual handler.
                bindings[uri] = null;
                removedOperation = RefreshSelection();
            }
            Invalidate(removedOperation);
            Changed?.Invoke();
        });
    }

    internal Task<UpdateOperationOutcome> CheckAsync(CancellationToken cancellationToken, bool preservePrepared = false,
        Func<bool>? downloadAutomatically = null) =>
        RunAsync(OperationKind.Check, cancellationToken, preservePrepared, downloadAutomatically);

    internal Task<UpdateOperationOutcome> DownloadAsync(CancellationToken cancellationToken) => RunAsync(OperationKind.Download, cancellationToken);

    /// <summary>Owns the prepared target through cancellation drain or acknowledged installer handoff.</summary>
    internal Task<UpdateOperationOutcome> ApplyAsync(CancellationToken cancellationToken) => RunAsync(OperationKind.Apply, cancellationToken);

    internal async Task<ApplicationUpdateChangelog> GetChangelogAsync(CultureInfo culture, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cultureName = culture.Name;
        NotesRequest request;
        List<Operation> invalidate;
        lock (gate)
        {
            var selection = sources.Selection;
            var binding = FindBinding(selection);
            var target = ReferenceEquals(state.Selection, selection) && ReferenceEquals(state.Binding, binding)
                ? state.Target : null;
            if (binding == null || target == null)
                throw new InvalidOperationException("Check must select an available update before reading notes.");
            if (notesIdentity == null || !ReferenceEquals(notesIdentity.Selection, selection) ||
                !ReferenceEquals(notesIdentity.Binding, binding) || !ReferenceEquals(notesIdentity.Target, target) ||
                !string.Equals(notesIdentity.CultureName, cultureName, StringComparison.OrdinalIgnoreCase))
                notesIdentity = new NotesIdentity(selection, binding, target, cultureName);
            request = new NotesRequest(notesIdentity,
                new Operation(selection, binding, new CancellationTokenSource(), cancellationToken));
            notes.Add(request);
            invalidate = RefreshSelection();
        }
        Invalidate(invalidate);

        var operation = request.Operation;
        var ownedToken = operation.Cancellation.Token;
        ApplicationUpdateChangelog? result = null;
        Exception? failure = null;
        CancellationTokenRegistration callerCancellation = default;
        try
        {
            callerCancellation = cancellationToken.Register(() =>
            {
                Operation? cancel;
                lock (gate) cancel = RequestCancellation(operation);
                Invalidate(cancel);
            });
            cancellationToken.ThrowIfCancellationRequested();
            ownedToken.ThrowIfCancellationRequested();
            if (IsCurrentNotes(request))
            {
                var task = operation.Binding.Handler.GetChangelogAsync(request.Identity.Target, culture, ownedToken);
                lock (gate) operation.HandlerTask = task;
                result = await task.ConfigureAwait(false);
            }
        }
        catch (Exception error) { failure = error; }
        finally
        {
            callerCancellation.Dispose();
            await DrainHandlerAsync(operation).ConfigureAwait(false);
            // Opaque author targets/handlers must never participate in equality under this lock.
            lock (gate) notes.RemoveAll(x => ReferenceEquals(x, request));
            operation.Cancellation.Dispose();
        }

        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentNotesLocked(request)) throw new OperationCanceledException(ownedToken);
            ownedToken.ThrowIfCancellationRequested();
        }
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return result!;
    }

    private async Task<UpdateOperationOutcome> RunAsync(OperationKind kind, CancellationToken cancellationToken,
        bool preservePrepared = false, Func<bool>? downloadAutomatically = null, State? expectedState = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Operation operation;
        IApplicationUpdateInfo? selectedTarget;
        State? committedCheckState = null;
        List<Operation> invalidate;
        lock (gate)
        {
            if (IsBusyLocked()) return UpdateOperationOutcome.Busy;
            var selection = sources.Selection;
            if (selection.Source == null)
                return selection.IsConfigured && selection.SelectedId == null
                    ? UpdateOperationOutcome.Disabled : UpdateOperationOutcome.NotConfigured;
            var binding = FindBinding(selection);
            if (binding == null)
            {
                return UpdateOperationOutcome.NotConfigured;
            }
            // Automatic preparation belongs to the exact completed Check, even if another
            // Check or registration used the same source URI in the meantime.
            if (expectedState != null && (!ReferenceEquals(state, expectedState) ||
                !ReferenceEquals(expectedState.Selection, selection) || !ReferenceEquals(expectedState.Binding, binding) ||
                expectedState.Phase != ApplicationUpdatePhase.UpdateAvailable))
                return UpdateOperationOutcome.Invalidated;
            var selected = ReferenceEquals(state.Selection, selection) && ReferenceEquals(state.Binding, binding)
                ? state : EmptyState(selection, binding);
            // A recurring request must not discard the same source's verified payload while waiting for explicit Apply.
            if (kind == OperationKind.Check && preservePrepared && selected.Phase == ApplicationUpdatePhase.ReadyToApply)
                return UpdateOperationOutcome.Completed;
            selectedTarget = selected.Target;
            if (kind != OperationKind.Check && selectedTarget == null)
                throw new InvalidOperationException($"Check must select an available update before {kind}.");
            if (kind == OperationKind.Apply && selected.Phase != ApplicationUpdatePhase.ReadyToApply)
                throw new InvalidOperationException("Download must prepare the selected update before Apply.");
            operation = new Operation(selection, binding, new CancellationTokenSource(), cancellationToken,
                kind == OperationKind.Download, kind == OperationKind.Apply);
            active = operation;
            state = kind == OperationKind.Check
                ? EmptyState(selection, binding) with { Phase = ApplicationUpdatePhase.Checking }
                : selected with { Phase = kind == OperationKind.Download ? ApplicationUpdatePhase.Downloading : ApplicationUpdatePhase.Applying,
                    Error = null, Progress = ProgressSnapshot.Empty,
                    NativeProgressVisible = kind == OperationKind.Download && ReferenceEquals(binding, nativeBinding) };
            invalidate = RefreshSelection();
        }
        Invalidate(invalidate);

        Exception? failure = null;
        var callerCancelled = false;
        CancellationTokenRegistration callerCancellation = default;
        try
        {
            callerCancellation = cancellationToken.Register(() =>
            {
                Operation? cancel;
                lock (gate) cancel = RequestCancellation(operation);
                Invalidate(cancel);
            });
            cancellationToken.ThrowIfCancellationRequested();
            Changed?.Invoke();
            if (IsCurrent(operation))
            {
                if (kind != OperationKind.Check)
                {
                    var reporter = new CycleProgressReporter(this, operation);
                    var task = kind == OperationKind.Download
                        ? operation.Binding.Handler.DownloadAsync(selectedTarget!, reporter, operation.Cancellation.Token)
                        : operation.Binding.Handler.ApplyAsync(selectedTarget!, reporter, operation.Cancellation.Token);
                    RegisterMutationTask(operation, reporter, task);
                    await task.ConfigureAwait(false);
                    if (operation.Accepted) return UpdateOperationOutcome.ApplyAccepted;
                    cancellationToken.ThrowIfCancellationRequested();
                    operation.Cancellation.Token.ThrowIfCancellationRequested();
                    // An arbitrary handler's completed Task is not an application-owned installation receipt.
                    if (kind == OperationKind.Apply)
                        throw new InvalidOperationException("The handler returned without an application-owned installation handoff.");
                    lock (gate)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (IsCurrentLocked(operation))
                            state = state with { Phase = ApplicationUpdatePhase.ReadyToApply,
                                Progress = state.Progress with { Indeterminate = false } };
                    }
                }
                else
                {
                    var source = operation.Selection.Source with { Uris = operation.Selection.Source.Uris?.ToArray() };
                    var task = operation.Binding.Handler.CheckAsync(source, operation.Cancellation.Token);
                    lock (gate) operation.HandlerTask = task;
                    var target = await task.ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    operation.Cancellation.Token.ThrowIfCancellationRequested();
                    // Author-owned getters run outside the ownership lock too.
                    var available = target.IsUpdateAvailable;
                    var installed = target.InstalledVersionText;
                    var version = available ? target.TargetVersionText : string.Empty;
                    var nativeUpdate = (target as NativeApplicationUpdateHandler.NativeUpdateInfo)?.Value;
                    lock (gate)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (IsCurrentLocked(operation))
                            state = committedCheckState = new State(operation.Selection, operation.Binding, available ? target : null,
                                installed, version, available ? ApplicationUpdatePhase.UpdateAvailable : ApplicationUpdatePhase.Idle,
                                null, ProgressSnapshot.Empty, nativeUpdate);
                    }
                }
                Changed?.Invoke();
            }
        }
        catch (Exception error)
        {
            failure = error;
            lock (gate)
            {
                if (operation.Accepted) state = state with { Error = error.Message };
                if (!operation.Accepted && IsCurrentLocked(operation))
                    state = EmptyState(operation.Selection, operation.Binding) with
                    {
                        Phase = error is OperationCanceledException && cancellationToken.IsCancellationRequested
                            // Apply remains in progress until the admitted cancellation callbacks drain below.
                            ? kind == OperationKind.Apply ? ApplicationUpdatePhase.Applying : ApplicationUpdatePhase.Idle
                            : ApplicationUpdatePhase.Failed,
                        Error = error is OperationCanceledException && cancellationToken.IsCancellationRequested ? null : error.Message
                    };
                invalidate = RefreshSelection();
            }
            // Clearing the target invalidates its notes before waiting for mutation
            // callbacks: those callbacks may themselves be awaiting the obsolete notes.
            Invalidate(invalidate);
            Changed?.Invoke();
        }
        finally
        {
            // Close the caller callback admission first. It only schedules owned cancellation
            // work; author callbacks run in that work and are acknowledged below.
            callerCancellation.Dispose();
            await DrainHandlerAsync(operation).ConfigureAwait(false);
            lock (gate)
            {
                // Decide the terminal caller outcome after actual drain, before a new operation
                // can enter. Cancellation delivery through the worker may have been delayed.
                callerCancelled = !operation.Accepted && cancellationToken.IsCancellationRequested;
                if (callerCancelled && IsCurrentLocked(operation))
                    state = EmptyState(operation.Selection, operation.Binding);
                // A committed helper owns the terminal plan until this process exits.
                if (!operation.Accepted) active = null;
                invalidate = RefreshSelection();
            }
            Invalidate(invalidate);
            operation.Cancellation.Dispose();
            if (!operation.Accepted) Changed?.Invoke();
        }

        if (operation.Accepted && failure == null) return UpdateOperationOutcome.ApplyAccepted;
        if (!IsCurrent(operation)) return UpdateOperationOutcome.Invalidated;
        if (callerCancelled) throw new OperationCanceledException(cancellationToken);
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        // Read the application setting outside the owner lock, after the Check handler and
        // cancellation callbacks have drained. Admission below revalidates the captured state.
        if (committedCheckState?.Phase == ApplicationUpdatePhase.UpdateAvailable && downloadAutomatically?.Invoke() == true)
            return await RunAsync(OperationKind.Download, cancellationToken, expectedState: committedCheckState).ConfigureAwait(false);
        return UpdateOperationOutcome.Completed;
    }

    private async Task DrainHandlerAsync(Operation operation)
    {
        Task? cancellationFinished;
        lock (gate)
        {
            operation.HandlerFinished = true;
            operation.ProvisionalProgress = null;
            cancellationFinished = operation.CancellationFinished?.Task;
        }
        // The actual handler has returned. Wait for every admitted cancellation callback
        // before releasing its CTS; neither author code nor awaits run under the owner lock.
        if (cancellationFinished != null) await cancellationFinished.ConfigureAwait(false);
    }

    private ProgressSnapshot ReadProgress(Operation operation)
    {
        lock (gate) return operation.Progress;
    }

    private void RegisterMutationTask(Operation operation, CycleProgressReporter reporter, Task? task)
    {
        var publish = false;
        var measured = false;
        lock (gate)
        {
            operation.HandlerTask = task;
            operation.HandlerTaskRegistered = true;
            var provisional = operation.ProvisionalProgress;
            operation.ProvisionalProgress = null;
            if (provisional != null && CanReportLocked(operation))
            {
                measured = !provisional.Indeterminate || provisional.Percent != operation.Progress.Percent;
                PublishProgressLocked(operation, provisional);
                publish = true;
            }
        }
        if (!publish) return;
        reporter.NotifyProgress(measured);
        Changed?.Invoke();
    }

    private bool CanReportLocked(Operation operation) => ReferenceEquals(active, operation) && IsCurrentLocked(operation) &&
        !operation.Accepted && !operation.HandlerFinished && operation.CancellationFinished == null &&
        !operation.CallerToken.IsCancellationRequested && !operation.Cancellation.IsCancellationRequested &&
        (!operation.HandlerTaskRegistered || operation.HandlerTask?.IsCompleted == false);

    private void PublishProgressLocked(Operation operation, ProgressSnapshot progress)
    {
        operation.Progress = progress;
        state = state with { Progress = progress };
    }

    private void ReportProgress(Operation operation, CycleProgressReporter reporter, string? stage, double? percent)
    {
        lock (gate)
        {
            if (!CanReportLocked(operation)) return;
            if (!operation.HandlerTaskRegistered)
            {
                // Synchronous author reports belong to the invocation. Concurrent reports
                // remain provisional until the exact returned task is known to be pending;
                // a completed task cannot leak a late report through the registration gap.
                operation.ProvisionalProgress = MergeProgress(operation.ProvisionalProgress ?? operation.Progress, stage, percent);
                if (Environment.CurrentManagedThreadId != operation.InvocationThreadId) return;
            }
            PublishProgressLocked(operation, MergeProgress(operation.Progress, stage, percent));
        }
        reporter.NotifyProgress(percent.HasValue);
        Changed?.Invoke();
    }

    private static ProgressSnapshot MergeProgress(ProgressSnapshot previous, string? stage, double? percent) =>
        new(stage ?? previous.Stage, percent ?? previous.Percent, !percent.HasValue);

    private void SelectionChanged()
    {
        List<Operation> invalidate;
        lock (gate) invalidate = RefreshSelection();
        Invalidate(invalidate);
        Changed?.Invoke();
    }

    // Called only under gate. The atomic selection is read without acquiring the provider lock.
    private List<Operation> RefreshSelection()
    {
        var selection = sources.Selection;
        var binding = FindBinding(selection);
        if (active?.Accepted != true && (!ReferenceEquals(state.Selection, selection) || !ReferenceEquals(state.Binding, binding)))
            state = EmptyState(selection, binding);
        var invalidate = new List<Operation>();
        if (active != null && !IsCurrentLocked(active) && RequestCancellation(active) is { } mutation)
            invalidate.Add(mutation);
        if (notesIdentity != null && (!ReferenceEquals(notesIdentity.Selection, selection) ||
            !ReferenceEquals(notesIdentity.Binding, binding) || !ReferenceEquals(notesIdentity.Target, state.Target)))
            notesIdentity = null;
        foreach (var request in notes)
            if (!IsCurrentNotesLocked(request) && RequestCancellation(request.Operation) is { } note)
                invalidate.Add(note);
        return invalidate;
    }

    private static Operation? RequestCancellation(Operation operation)
    {
        if (operation.Accepted || operation.HandlerFinished || operation.CancellationFinished != null) return null;
        operation.CancellationFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return operation;
    }

    private static void Invalidate(Operation? operation)
    {
        if (operation == null) return;
        // Do not run author cancellation callbacks under the provider's synchronous notification
        // lock either. This task remains owned and is awaited before the operation is released.
        _ = Task.Run(() =>
        {
            try { operation.Cancellation.Cancel(); }
            catch (Exception error) { Log.Warn("Registered updater cancellation callback failed", error); }
            finally { operation.CancellationFinished!.TrySetResult(true); }
        });
    }

    private static void Invalidate(IEnumerable<Operation> operations)
    {
        foreach (var operation in operations) Invalidate(operation);
    }

    private State ReadState()
    {
        lock (gate)
        {
            if (active?.Accepted == true) return state;
            var selection = sources.Selection;
            var binding = FindBinding(selection);
            return ReferenceEquals(state.Selection, selection) && ReferenceEquals(state.Binding, binding)
                ? state : EmptyState(selection, binding);
        }
    }

    private bool IsCurrent(Operation operation) { lock (gate) return IsCurrentLocked(operation); }
    private bool IsCurrentLocked(Operation operation) => operation.Accepted ||
        ReferenceEquals(sources.Selection, operation.Selection) && ReferenceEquals(FindBinding(operation.Selection), operation.Binding);

    private void AcceptInstallation(Operation operation, Action commitHelper)
    {
        lock (gate)
        {
            if (!ReferenceEquals(active, operation) || !operation.IsApply || !IsCurrentLocked(operation) ||
                operation.CancellationFinished != null || operation.CallerToken.IsCancellationRequested ||
                operation.Cancellation.IsCancellationRequested)
                throw new OperationCanceledException(operation.Cancellation.Token);
            if (operation.Accepted) throw new InvalidOperationException("This installation was already accepted.");
            // This callback is the app-owned helper's bounded commit, never author code.
            // Serialize it with cancellation admission and source invalidation.
            commitHelper();
            operation.Accepted = true;
            state = state with { Phase = ApplicationUpdatePhase.HandedOff, Error = null };
        }
        // The installer invokes this on the dispatcher. Observers are outside the gate and
        // may dispose their scope, cancel their caller and choose the next cycle's source.
        HandedOff?.Invoke();
    }

    private bool IsCurrentNotes(NotesRequest request) { lock (gate) return IsCurrentNotesLocked(request); }
    private bool IsCurrentNotesLocked(NotesRequest request) => ReferenceEquals(notesIdentity, request.Identity) &&
        IsCurrentLocked(request.Operation) && ReferenceEquals(state.Target, request.Identity.Target);

    private bool IsAuthorSource(UpdateSourceSelection selection) => selection.Source?.Uris?.Any(x =>
        Uri.TryCreate(x, UriKind.Absolute, out var uri) && bindings.ContainsKey(uri)) == true;

    private Binding? FindBinding(UpdateSourceSelection selection)
    {
        foreach (var value in selection.Source?.Uris ?? Array.Empty<string>())
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && bindings.TryGetValue(uri, out var binding)) return binding;
        return selection.Source == null ? null : nativeBinding;
    }

    private static State EmptyState(UpdateSourceSelection selection, Binding? binding) => new(selection, binding, null, string.Empty, string.Empty,
        selection.Source == null
            ? selection.IsConfigured && selection.SelectedId == null ? ApplicationUpdatePhase.Disabled : ApplicationUpdatePhase.NotConfigured
            : binding == null ? ApplicationUpdatePhase.NotConfigured : ApplicationUpdatePhase.Idle, null, ProgressSnapshot.Empty);

    private sealed record Binding(IApplicationUpdateHandler Handler);
    private sealed record NotesIdentity(UpdateSourceSelection Selection, Binding Binding, IApplicationUpdateInfo Target, string CultureName);
    private sealed record NotesRequest(NotesIdentity Identity, Operation Operation);
    private sealed record State(UpdateSourceSelection Selection, Binding? Binding, IApplicationUpdateInfo? Target,
        string Installed, string Version, ApplicationUpdatePhase Phase, string? Error, ProgressSnapshot Progress,
        IPoeUpdateInfo? NativeUpdate = null, bool NativeProgressVisible = false);

    private sealed record ProgressSnapshot(string Stage, double Percent, bool Indeterminate)
    {
        public static readonly ProgressSnapshot Empty = new(string.Empty, 0, false);
    }

    /// <summary>Chooses the handler call within the existing single mutation owner.</summary>
    private enum OperationKind { Check, Download, Apply }

    private sealed record Operation(UpdateSourceSelection Selection, Binding Binding, CancellationTokenSource Cancellation, CancellationToken CallerToken,
        bool IsDownload = false, bool IsApply = false)
    {
        public bool Accepted { get; set; }
        public Task? HandlerTask { get; set; }
        public bool HandlerTaskRegistered { get; set; }
        public int InvocationThreadId { get; } = Environment.CurrentManagedThreadId;
        public bool HandlerFinished { get; set; }
        public TaskCompletionSource<bool>? CancellationFinished { get; set; }
        public ProgressSnapshot Progress { get; set; } = ProgressSnapshot.Empty;
        public ProgressSnapshot? ProvisionalProgress { get; set; }
    }

    /// <summary>Author-visible numeric/typed progress for one operation; all publication belongs to its owner.</summary>
    private sealed class CycleProgressReporter : DisposableReactiveObject, IApplicationUpdateProgressReporter, IApplicationUpdateAcceptance
    {
        private readonly ApplicationUpdateHandlerRegistrator owner;
        private readonly Operation operation;

        public CycleProgressReporter(ApplicationUpdateHandlerRegistrator owner, Operation operation)
        {
            this.owner = owner;
            this.operation = operation;
        }

        public string TaskName => owner.ReadProgress(operation).Stage;
        public double Weight => 1;
        public double ProgressPercent => owner.ReadProgress(operation).Percent;
        public void Update(double progressPercent) => owner.ReportProgress(operation, this, null, progressPercent);
        public void ReportStage(string stage, double? percent) => owner.ReportProgress(operation, this, stage, percent);
        public void Accept(Action commitHelper) => owner.AcceptInstallation(operation, commitHelper);

        internal void NotifyProgress(bool measured)
        {
            this.RaisePropertyChanged(nameof(TaskName));
            if (measured) this.RaisePropertyChanged(nameof(ProgressPercent));
        }
    }
}
