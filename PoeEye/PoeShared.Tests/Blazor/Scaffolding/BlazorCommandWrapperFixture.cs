using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Modularity;

namespace PoeShared.Tests.Blazor.Scaffolding;

public class BlazorCommandWrapperFixture : FixtureBase
{
    [Test]
    public void ShouldNotThrowWhenDisposed()
    {
        //Given
        var context = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);
        
        var canExecute = new Subject<bool>();
        var command = BlazorCommandWrapper.Create(() => { }, canExecute);
        command.Dispose();

        //When
        var action = () => canExecute.OnNext(true);

        //Then
        action.ShouldNotThrow();
    }
    
    [Test]
    [Timeout(1000)]
    public void ShouldNotThrowWhenDisposedDuringAction()
    {
        //Given
        var context = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        var startSignal = new ManualResetEventSlim(false);
        var completionSignal = new ManualResetEventSlim(false);
        var command = BlazorCommandWrapper.Create(() =>
        {
            startSignal.Set();
            completionSignal.Wait();
            throw new ApplicationException();
        });

        //When
        Task.Run(() => command.ExecuteAsync());
        startSignal.Wait();
        command.Dispose();
        completionSignal.Set();

        //Then
        GC.Collect();
    }
}