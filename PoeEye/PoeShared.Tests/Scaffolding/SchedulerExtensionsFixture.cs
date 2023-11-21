using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Modularity;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class SchedulerExtensionsFixtureTests : FixtureBase
{
    [Test]
    public async Task ShouldRunOnScheduler()
    {
        //Given
        var scheduler = SchedulerProvider.Instance.GetOrAdd("SchedulerExtensionsFixtureDispatcher");
        var completedEvent = new TaskCompletionSource<bool>();

        //When
        scheduler.IsOnScheduler().ShouldBeFalse();
        Observable.Start(() =>
        {
            completedEvent.SetResult(scheduler.IsOnScheduler());
        }, scheduler);

        //Then
        completedEvent.Task.Result.ShouldBeTrue();
    }
    
    [Test]
    public async Task ShouldStartOnScheduler()
    {
        //Given
        var scheduler = SchedulerProvider.Instance.GetOrAdd("SchedulerExtensionsFixtureDispatcher");
        var completedEvent = new TaskCompletionSource<bool>();

        //When
        scheduler.IsOnScheduler().ShouldBeFalse();
        Observable.Start(() =>
        {
            completedEvent.SetResult(scheduler.IsOnScheduler());
        }, scheduler);

        //Then
        completedEvent.Task.Result.ShouldBeTrue();
    }
    
    [Test]
    public async Task ShouldNotStartAsyncOnScheduler()
    {
        //Given
        var scheduler = SchedulerProvider.Instance.GetOrAdd("SchedulerExtensionsFixtureDispatcher");
        var completedEvent = new TaskCompletionSource<bool>();

        //When
        scheduler.IsOnScheduler().ShouldBeFalse();
        Observable.StartAsync(async () =>
        {
            completedEvent.SetResult(scheduler.IsOnScheduler());
        }, scheduler);

        //Then
        completedEvent.Task.Result.ShouldBeFalse();
    }
    
    [Test]
    public async Task ShouldStartAsyncAndReturnOnScheduler()
    {
        //Given
        var scheduler = SchedulerProvider.Instance.GetOrAdd("SchedulerExtensionsFixtureDispatcher");
        var completedEvent = new TaskCompletionSource<bool>();

        //When
        scheduler.IsOnScheduler().ShouldBeFalse();
        Observable.Start(async () =>
        {
            try
            {
                Log.Info("Entering");
                scheduler.IsOnScheduler().ShouldBeTrue();
                Log.Info("Waiting");
                await Task.Delay(1000);
                Log.Info("Exiting");
                scheduler.IsOnScheduler().ShouldBeTrue();
                completedEvent.SetResult(true);
            }
            catch (Exception e)
            {
                completedEvent.SetException(e);
            }
        }, scheduler);

        //Then
        for (int i = 0; i < 5; i++)
        {
            scheduler.Run(() => { Log.Info($"Index: #{i}"); });
            Thread.Sleep(100);
        }
        completedEvent.Task.Result.ShouldBeTrue();
    }
    
    [Test]
    public async Task ShouldRunAsyncAndReturnOnScheduler()
    {
        //Given
        var scheduler = SchedulerProvider.Instance.GetOrAdd("SchedulerExtensionsFixtureDispatcher");
        var completedEvent = new TaskCompletionSource<bool>();

        //When
        scheduler.IsOnScheduler().ShouldBeFalse();
        Observable.Start(async () =>
        {
            try
            {
                Log.Info("Entering");
                scheduler.IsOnScheduler().ShouldBeTrue();
                Log.Info("Waiting");
                await Task.Delay(1000);
                Log.Info("Exiting");
                scheduler.IsOnScheduler().ShouldBeTrue();
                completedEvent.SetResult(true);
            }
            catch (Exception e)
            {
                completedEvent.SetException(e);
            }
        }, scheduler);

        //Then
        completedEvent.Task.Result.ShouldBeTrue();
    }
}