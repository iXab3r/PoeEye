using System.Reactive.Subjects;
using System.Windows.Forms;
using Moq;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Tests.Helpers;
using PoeShared.UI;

namespace PoeShared.Tests.UI.Hotkeys
{
    [TestFixture]
    internal class HotkeyTrackerFixture
    {
        private Mock<IClock> clock;
        private Mock<IKeyboardEventsSource> eventSource;
        private Mock<IWindowTracker> mainWindowTracker;
        private Mock<IAppArguments> appArguments;

        private ISubject<KeyEventArgs> whenKeyDown;
        private ISubject<KeyEventArgs> whenKeyUp;
        private ISubject<KeyPressEventArgs> whenKeyPress;
        private ISubject<MouseEventArgs> whenMouseDown;
        private ISubject<MouseEventArgs> whenMouseUp;
        private ISubject<MouseEventArgs> whenMouseWheel;
        private ISubject<MouseEventArgs> whenMouseMove;
        
        [SetUp]
        public void SetUp()
        {
            clock = new Mock<IClock>();

            appArguments = new Mock<IAppArguments>();
            
            eventSource = new Mock<IKeyboardEventsSource>();
            whenKeyDown = eventSource.SetupGet(x => x.WhenKeyDown).ReturnsPublisher();
            whenKeyUp = eventSource.SetupGet(x => x.WhenKeyUp).ReturnsPublisher();
            whenKeyPress = eventSource.SetupGet(x => x.WhenKeyPress).ReturnsPublisher();
            whenMouseDown = eventSource.SetupGet(x => x.WhenMouseDown).ReturnsPublisher();
            whenMouseUp = eventSource.SetupGet(x => x.WhenMouseUp).ReturnsPublisher();
            whenMouseWheel = eventSource.SetupGet(x => x.WhenMouseWheel).ReturnsPublisher();
            whenMouseMove = eventSource.SetupGet(x => x.WhenMouseMove).ReturnsPublisher();
            
            mainWindowTracker = new Mock<IWindowTracker>();
        }

        [Test]
        public void ShouldCreate()
        {
            //Given
            //When
            

            //Then

        }
        
        public HotkeyTracker CreateInstance()
        {
            return new HotkeyTracker(clock.Object, appArguments.Object, eventSource.Object, mainWindowTracker.Object);
        }
    }
}