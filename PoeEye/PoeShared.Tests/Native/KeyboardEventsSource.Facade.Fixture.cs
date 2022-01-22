using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using WindowsHook;
using Moq;
using NUnit.Framework;
using PoeShared.Native;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Tests.Native;

[TestFixture]
public class KeyboardEventsSourceFacadeFixture
{
    private static readonly IFluentLog Log = typeof(KeyboardEventsSourceFacadeFixture).PrepareLogger();
        
    private Mock<IClock> clock;
    private Mock<IKeyboardMouseEventsProvider> eventsFactory;
        
    private IScheduler inputScheduler;
    private Mock<IObservable<IKeyboardMouseEvents>> eventSourceMock;

    [SetUp]
    public void SetUp()
    {
        inputScheduler = Scheduler.Immediate;
        clock = new Mock<IClock>();
        eventsFactory = new Mock<IKeyboardMouseEventsProvider>();

        eventSourceMock = new Mock<IObservable<IKeyboardMouseEvents>>();
        eventSourceMock
            .Setup(x => x.Subscribe(It.IsAny<IObserver<IKeyboardMouseEvents>>()))
            .Returns(Disposable.Empty);
        eventsFactory.SetupGet(x => x.System).Returns(eventSourceMock.Object);
    }
        
        
    [Test]
    public void ShouldSubscribeToFactoryOnlyOnce()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.WhenKeyDown.Subscribe();

        //Then
        eventSourceMock.Verify(x => x.Subscribe(It.IsAny<IObserver<IKeyboardMouseEvents>>()), Times.Once);
    }

    [Test]
    public void ShouldCreateOnlyOnSubscription()
    {
        //Given
        //When
        var instance = CreateInstance();

        //Then
        eventSourceMock.Verify(x => x.Subscribe(It.IsAny<IObserver<IKeyboardMouseEvents>>()), Times.Never);
    }

    [Test]
    public void ShouldNotDoMultipleSubscriptionsForSameEvent()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.WhenKeyDown.Subscribe();
        instance.WhenKeyDown.Subscribe();

        //Then
        eventSourceMock.Verify(x => x.Subscribe(It.IsAny<IObserver<IKeyboardMouseEvents>>()), Times.Once);
    }
        
    private KeyboardEventsSource CreateInstance()
    {
        return new KeyboardEventsSource(eventsFactory.Object, clock.Object, Scheduler.Immediate);
    }
}