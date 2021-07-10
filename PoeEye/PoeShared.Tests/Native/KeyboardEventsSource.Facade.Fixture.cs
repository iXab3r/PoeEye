using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using log4net;
using Microsoft.Reactive.Testing;
using Moq;
using NUnit.Framework;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Shouldly;

namespace PoeShared.Tests.Native
{
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
        public void ShouldPropagateKeyboard()
        {
            //Given
            var eventsSource = new Mock<IKeyboardMouseEvents>();
            eventsFactory.Setup(x => x.System).Returns(Observable.Return(eventsSource.Object));
            
            var instance = CreateInstance();
            var readyToCapture = new ManualResetEvent(false);
            var captured = new ManualResetEvent(false);
            var processingTask = Task.Run(() =>
            {
                Log.Debug("Awaiting for input event");
                instance.WhenKeyDown.Where(x => x.KeyCode == Keys.A).Take(1).Subscribe(x => captured.Set());
                readyToCapture.Set();
                captured.WaitOne();
                Log.Debug("Completing task");
            });

            //When
            readyToCapture.WaitOne();
            Log.Debug("Raising input event");
            eventsSource.Raise(x => x.KeyDown -= null, new KeyEventArgs(Keys.A));

            //Then
            processingTask.Wait(3000).ShouldBe(true);
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
            return new KeyboardEventsSource(eventsFactory.Object, inputScheduler, clock.Object);
        }
    }
}