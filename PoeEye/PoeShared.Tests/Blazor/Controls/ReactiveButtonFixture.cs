using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Blazor.Controls;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Tests.Blazor.Controls;

public class ReactiveButtonFixture : FixtureBase
{
    /// <summary>
    /// WHAT: Repeated clicks while a ReactiveButton command is already running must be ignored.
    /// HOW: Starts a long-running command through the private click handler, invokes the handler again while busy, and verifies only one execution occurs.
    /// </summary>
    [Test]
    public async Task ShouldIgnoreDuplicateExecute_WhenCommandIsBusy()
    {
        // Given
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        try
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var allowCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var executionCount = 0;
            var button = new ReactiveButton();
            var command = BlazorCommandWrapper.Create(async () =>
            {
                Interlocked.Increment(ref executionCount);
                started.TrySetResult();
                await allowCompletion.Task;
            });
            SetCommand(button, command);

            var firstExecution = Execute(button);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(1));
            SpinWait.SpinUntil(() => button.IsBusy, TimeSpan.FromSeconds(1)).ShouldBeTrue();

            // When
            var secondExecution = Execute(button);

            // Then
            await secondExecution.WaitAsync(TimeSpan.FromSeconds(1));
            executionCount.ShouldBe(1);

            allowCompletion.SetResult();
            await firstExecution.WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private static Task Execute(ReactiveButton button)
    {
        var executeMethod = typeof(ReactiveButton).GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic);
        executeMethod.ShouldNotBeNull();
        return (Task) executeMethod.Invoke(button, Array.Empty<object>());
    }

    private static void SetCommand(ReactiveButton button, ICommandWrapper command)
    {
        var property = typeof(ReactiveButton).GetProperty(nameof(ReactiveButton.CommandEx));
        property.ShouldNotBeNull();
        property.SetValue(button, command);
    }
}
