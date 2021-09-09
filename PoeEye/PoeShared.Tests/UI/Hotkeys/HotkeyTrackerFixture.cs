using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows.Forms;
using System.Windows.Input;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Tests.Helpers;
using PoeShared.Tests.Scaffolding;
using PoeShared.UI;
using Shouldly;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

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
        private Fixture fixture;
        
        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            
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
            
            fixture.Register(() => clock.Object);
            fixture.Register(() => eventSource.Object);
            fixture.Register(() => appArguments.Object);
            fixture.Register<IScheduler>(() => Scheduler.Immediate);
            
            mainWindowTracker = fixture.Freeze<IWindowTracker>().GetMock();
            mainWindowTracker.SetupGet(x => x.ActiveProcessId).Returns(1);
            mainWindowTracker.SetupGet(x => x.ExecutingProcessId).Returns(2);
        }

        [Test]
        public void ShouldCreate()
        {
            //Given
            //When
            //Then
        }

        [Test]
        public void ShouldSubscribeToSingleHotkey()
        {
            //Given
            var instance = CreateInstance();
            instance.Hotkey = new HotkeyGesture(Key.A);
            instance.HotkeyMode = HotkeyMode.Click;
            instance.IsActive.ShouldBe(false);

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(true);
        }

        [Test]
        public void ShouldSubscribeToMultipleHotkeysWhenModeIsClick()
        {
            //Given
            var instance = CreateInstance();
            instance.Add(new HotkeyGesture(Key.A));
            instance.Add(new HotkeyGesture(Key.B));
            instance.HotkeyMode = HotkeyMode.Click;

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //Then
            whenKeyDown.OnNext(new KeyEventArgs(Keys.B));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.B));
            instance.IsActive.ShouldBe(false);
        }
        
        [Test]
        [Theory]
        public void ShouldNotHandleKeyUpIfThereWereNoKeyDownBefore(HotkeyMode hotkeyMode)
        {
            //Given
            var instance = CreateInstance();
            instance.Add(new HotkeyGesture(Key.A));
            instance.HotkeyMode = hotkeyMode;

            //When
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(false);
        }
        
        [Test]
        public void ShouldSubscribeToMultipleHotkeysWhenModeIsHold()
        {
            var instance = CreateInstance();
            instance.Add(new HotkeyGesture(Key.A));
            instance.Add(new HotkeyGesture(Key.B));
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.IsActive.ShouldBe(false);

            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.B));
            instance.IsActive.ShouldBe(true);
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(false);
            whenKeyUp.OnNext(new KeyEventArgs(Keys.B));
            instance.IsActive.ShouldBe(false);
        }

        [Test]
        public void ShouldSetIsActualOnHotkeyUpdateWhenModeIsClick()
        {
            //Given
            var instance = CreateInstance();
            instance.Hotkey = new HotkeyGesture(Key.A);
            instance.HotkeyMode = HotkeyMode.Click;

            //Then
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(false);
            
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);
            
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);
            
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(false);
        }

        [Test]
        public void ShouldSetIsActualOnHotkeyUpdateWhenModeIsHold()
        {
            //Given
            var instance = CreateInstance();
            instance.Hotkey = new HotkeyGesture(Key.A);
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.IsActive.ShouldBe(false);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);
            
            //When
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(false);
        }
        
        [Test]
        public void ShouldResetIsActualOnKeyChange()
        {
            //Given
            var instance = CreateInstance();
            instance.Hotkey = new HotkeyGesture(Key.A);
            instance.HotkeyMode = HotkeyMode.Click;
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //When
            instance.Hotkey = new HotkeyGesture(Key.B);

            //Then
            instance.IsActive.ShouldBe(false);
        }
        
        [Test]
        public void ShouldResetIsActualOnModeChange()
        {
            //Given
            var instance = CreateInstance();
            instance.Hotkey = new HotkeyGesture(Key.A);
            instance.HotkeyMode = HotkeyMode.Click;
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //When
            instance.HotkeyMode = HotkeyMode.Hold;

            //Then
            instance.IsActive.ShouldBe(false);
        }

        [Test]
        public void ShouldProcessModifiers()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Click;
            instance.Hotkey = new HotkeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt);

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.Control | Keys.A | Keys.Alt));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.Control | Keys.A | Keys.Alt));

            //Then
            instance.IsActive.ShouldBe(true);
        }
        
        [Test]
        [TestCase(Keys.Alt)]
        [TestCase(Keys.Control)]
        [TestCase(Keys.A | Keys.Control | Keys.Alt)]
        // [TestCase(Keys.A)] // in real world when you're releasing A if Ctrl+A is pressed signal is Ctrl+A UP, not A UP
        public void ShouldProcessHotkeyWhenModifierReleased(Keys keyToRelease)
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.Hotkey = new HotkeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.Control | Keys.A | Keys.Alt));
            instance.IsActive.ShouldBe(true);

            //When
            whenKeyUp.OnNext(new KeyEventArgs(keyToRelease));

            //Then
            instance.IsActive.ShouldBe(false);
        }
        
        [Test]
        public void ShouldProcessHotkeyUpDownWhenModifierReleased()
        {
            // The problem was the following:
            // Hotkey is Shift+E, user presses Shift+E, activates hotkey. SendInput is linked to this tracker and starts to spam some other key, e.g. W
            // then the following events are generated: Shift+W DOWN, SHIFT UP(this breaks Shift+E), SHIFT+W UP
            // inner logic processed Shift release as Shift+E release and suppressed it
            // and in the end we got permanent Shift DOWN state even though pressed combination had nothing to do with Shift+E aside from sharing a modifier
            
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Click;
            instance.Hotkey = new HotkeyGesture(Key.E, ModifierKeys.Shift);
            instance.SuppressKey = true;

            // user started with pressing down modifier key
            var keyDown0 = new KeyEventArgs(Keys.ShiftKey);
            whenKeyDown.OnNext(keyDown0);
            instance.IsActive.ShouldBe(false);
            keyDown0.Handled.ShouldBe(false);

            // then user pressed and released additional key, now it's (modifier + key)
            var keyDown1 = new KeyEventArgs(Keys.Shift | Keys.E);
            whenKeyDown.OnNext(keyDown1);
            instance.IsActive.ShouldBe(false);
            keyDown1.Handled.ShouldBe(true);
            var keyUp1 = new KeyEventArgs(Keys.Shift | Keys.E);
            whenKeyUp.OnNext(keyUp1);
            instance.IsActive.ShouldBe(true);
            keyUp1.Handled.ShouldBe(true);
            
            // then user pressed some other key still holding the modifier 
            var keyDown2 = new KeyEventArgs(Keys.Shift | Keys.W);
            whenKeyDown.OnNext(keyDown2);
            instance.IsActive.ShouldBe(true);
            keyDown2.Handled.ShouldBe(false);
            var keyUp2 = new KeyEventArgs(Keys.Shift | Keys.W);
            whenKeyUp.OnNext(keyUp2);
            instance.IsActive.ShouldBe(true);
            keyUp2.Handled.ShouldBe(false);
            
            // then presses the same key again still holding the same modifier
            var keyDown3 = new KeyEventArgs(Keys.Shift | Keys.E);
            whenKeyDown.OnNext(keyDown3);
            instance.IsActive.ShouldBe(true);
            keyDown3.Handled.ShouldBe(true);
            
            //When user releases modifier
            var keyUp3 = new KeyEventArgs(Keys.ShiftKey);
            whenKeyUp.OnNext(keyUp3);
            
            //Then tracker should NOT suppress this modifier
            instance.IsActive.ShouldBe(false);
            keyUp3.Handled.ShouldBe(false);
        }

        [Test]
        [TestCase(Key.None, null, null, ModifierKeys.None, false)]
        [TestCase(Key.A, null, null, ModifierKeys.None, false)]
        [TestCase(null, MouseButton.Left, null, ModifierKeys.None, false)]
        [TestCase(null, null, MouseWheelAction.WheelDown, ModifierKeys.None, false)]
        [TestCase(null, null, MouseWheelAction.None, ModifierKeys.None, false)]
        [TestCase(null, null, MouseWheelAction.None, ModifierKeys.Alt, true)]
        [TestCase(null, MouseButton.Left, null, ModifierKeys.Alt, true)]
        [TestCase(Key.A, null, null, ModifierKeys.Alt, true)]
        [TestCase(Key.A, null, null, ModifierKeys.Control, true)]
        [TestCase(Key.A, null, null, ModifierKeys.Shift, true)]
        [TestCase(Key.A, null, null, ModifierKeys.Windows, true)]
        public void ShouldCalculateHasModifiersForSingleHotkey(Key? key, MouseButton? mouseButton, MouseWheelAction? mouseWheelAction, ModifierKeys modifierKeys, bool expectedHasModifiers)
        {
            //Given
            var instance = CreateInstance();
            instance.HasModifiers.ShouldBe(false);

            //When
            instance.Add(CreateGesture(key, mouseButton, mouseWheelAction, modifierKeys));

            //Then
            instance.HasModifiers.ShouldBe(expectedHasModifiers);
        }

        [Test]
        public void ShouldRecalculateHasModifiersOnKeyRemoval()
        {
            //Given
            var instance = CreateInstance();
            instance.Add(new HotkeyGesture(Key.A, ModifierKeys.Alt));
            instance.HasModifiers.ShouldBe(true);

            //When
            instance.Clear();

            //Then
            instance.HasModifiers.ShouldBe(false);
        }

        [Test]
        public void ShouldResetIgnoreModifiersWhenHotkeyHasModifiers()
        {
            //Given
            var instance = CreateInstance();
            instance.IgnoreModifiers = true;

            //When
            instance.Add(new HotkeyGesture(Key.A, ModifierKeys.Alt));

            //Then
            instance.IgnoreModifiers.ShouldBe(false);
        }
        
        [Test]
        public void ShouldProcessKeyWithIgnoreModifiersWhenModeIsClick()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Click;
            instance.IgnoreModifiers = true;
            instance.Add(new HotkeyGesture(Key.A));

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A | Keys.Control));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A | Keys.Control));

            //Then
            instance.IsActive.ShouldBe(true);
        }
        
        [Test]
        public void ShouldProcessKeyWithIgnoreModifiersWhenModeIsHold()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.IgnoreModifiers = true;
            instance.Add(new HotkeyGesture(Key.A));

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A | Keys.Control));

            //Then
            instance.IsActive.ShouldBe(true);
        }
        
        [TestCase(true, HotkeyMode.Hold, false)]
        [TestCase(false, HotkeyMode.Hold, false)]
        [TestCase(false, HotkeyMode.Click, false)]
        [TestCase(true, HotkeyMode.Click, true)]
        public void ShouldProcessKeyIfReleasedWithModifierWithIgnoreModifiersWhenModeIsHold(bool ignoreModifiers, HotkeyMode mode, bool expected)
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = mode;
            instance.IgnoreModifiers = ignoreModifiers;
            instance.Add(new HotkeyGesture(Key.A));

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A | Keys.Shift));

            //Then
            instance.IsActive.ShouldBe(expected);
        }
        
        [Test]
        public void ShouldResetSuppressKeyWhenMouseHotkeyIsAdded()
        {
            //Given
            var instance = CreateInstance();
            instance.SuppressKey = true;

            //When
            instance.Add(new HotkeyGesture(MouseButton.Left));

            //Then
            instance.SuppressKey.ShouldBe(false);
        }

        [Test]
        public void ShouldNotProcessKeysWhenNotEnabledWhenModeIsClick()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Click;
            instance.Add(new HotkeyGesture(Key.A));
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //When
            instance.IsEnabled = false;
            instance.IsActive.ShouldBe(true);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(true);
        }
        
        [Test]
        public void ShouldNotProcessKeysWhenNotEnabledWhenModeIsHold()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.Add(new HotkeyGesture(Key.A));
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //When
            instance.IsEnabled = false;
            instance.IsActive.ShouldBe(true);
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(true);
        }

        [Test]
        [TestCase(false, false, true)]
        [TestCase(false, true, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, true)]
        public void ShouldNotProcessKeyIfMainWindowIsActiveWhenModeIsClick(bool handleApplicationKeys, bool sameProcess, bool expectedIsActive)
        {
            //Given
            var instance = CreateInstance();
            instance.HandleApplicationKeys = handleApplicationKeys;
            instance.HotkeyMode = HotkeyMode.Click;
            instance.Add(new HotkeyGesture(Key.A));
            if (sameProcess)
            {
                mainWindowTracker.SetupGet(x => x.ActiveProcessId).Returns(0);
                mainWindowTracker.SetupGet(x => x.ExecutingProcessId).Returns(0);
            }

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(expectedIsActive);
        }
        
        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ShouldProcessKeyIfMainWindowIsActiveWhenModeIsHold(bool handleApplicationKeys, bool sameProcess)
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.Add(new HotkeyGesture(Key.A));
            if (sameProcess)
            {
                mainWindowTracker.SetupGet(x => x.ActiveProcessId).Returns(0);
                mainWindowTracker.SetupGet(x => x.ExecutingProcessId).Returns(0);
            }

            //When
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));

            //Then
            instance.IsActive.ShouldBe(true);
        }

        [Test]
        public void ShouldSuppressKeyWhenModeIsClick()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Click;
            instance.Hotkey = new HotkeyGesture(Key.A);
            instance.SuppressKey = true;

            //When
            var keyDown = new KeyEventArgs(Keys.A);
            whenKeyDown.OnNext(keyDown);

            //Then
            keyDown.Handled.ShouldBe(true);
        }

        [Test]
        public void ShouldNotResetIsActiveWhenDisabledWhenModeIsHold()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.Hotkey = new HotkeyGesture(Key.A);
            var keyDown = new KeyEventArgs(Keys.A);
            whenKeyDown.OnNext(keyDown);
            instance.IsActive.ShouldBe(true);

            //When
            instance.IsEnabled = false;

            //Then
            instance.IsActive.ShouldBe(true);
        }
        
        [Test]
        public void ShouldNotResetIsActiveWhenDisabledWhenModeIsClick()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Click;
            instance.Hotkey = new HotkeyGesture(Key.A);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            whenKeyUp.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //When
            instance.IsEnabled = false;

            //Then
            instance.IsActive.ShouldBe(true);
        }

        [Test]
        public void ShouldResubscribeToKeyDownWhenReenabledWhenModeIsHold()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.Hotkey = new HotkeyGesture(Key.A);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);
            instance.IsEnabled = false;
            instance.Reset();

            //When
            instance.IsEnabled = true;
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            
            //Then
            instance.IsActive.ShouldBe(true);
        }

        [Test]
        public void ShouldResetIsActive()
        {
            //Given
            var instance = CreateInstance();
            instance.HotkeyMode = HotkeyMode.Hold;
            instance.Hotkey = new HotkeyGesture(Key.A);
            whenKeyDown.OnNext(new KeyEventArgs(Keys.A));
            instance.IsActive.ShouldBe(true);

            //When
            instance.Reset();

            //Then
            instance.IsActive.ShouldBe(false);
        }
        
        private static HotkeyGesture CreateGesture(Key? key, MouseButton? mouseButton, MouseWheelAction? mouseWheelAction, ModifierKeys modifierKeys)
        {
            var nonNull = (key != null ? 1 : 0) + (mouseButton != null ? 1 : 0) + (mouseWheelAction != null ? 1 : 0);
            if (nonNull > 1)
            {
                throw new ArgumentException($"Invalid arguments: {new {key, mouseButton, mouseWheelAction, modifierKeys}}");
            }

            if (key != null)
            {
                return new HotkeyGesture(key.Value, modifierKeys);
            } else if (mouseButton != null)
            {
                return new HotkeyGesture(mouseButton.Value, modifierKeys);
            } else if (mouseWheelAction != null)
            {
                return new HotkeyGesture(mouseWheelAction.Value, modifierKeys);
            }
            else
            {
                throw new ArgumentException("Something went wrong");
            }
        }
        
        public HotkeyTracker CreateInstance()
        {
            return fixture.Build<HotkeyTracker>().OmitAutoProperties().Create();
        }
    }
}