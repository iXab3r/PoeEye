using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Windows.Forms;
using System.Windows.Input;
using Moq;
using PoeShared.Native;
using PoeShared.Services;
using PoeShared.Tests.Helpers;
using PoeShared.UI;
using Shouldly;
using WindowsHook;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace PoeShared.Tests.UI.Hotkeys
{
    [TestFixture]
    public class UserInputBlockerFixture : FixtureBase
    {
        private Mock<IKeyboardEventsSource> eventSource;
        private Mock<ISharedResourceLatch> resourceLatch;
        private ISubject<KeyEventArgsExt> whenKey;
        private ISubject<MouseEventExtArgs> whenMouse;
        
        protected override void SetUp()
        {
            eventSource = new Mock<IKeyboardEventsSource>();
            whenKey = eventSource.SetupGet(x => x.WhenKeyRaw).ReturnsPublisher();
            whenMouse = eventSource.SetupGet(x => x.WhenMouseRaw).ReturnsPublisher();

            resourceLatch = new Mock<ISharedResourceLatch>();
            var latchState = false;
            resourceLatch.SetupGet(x => x.IsBusy).Returns(() => latchState);
            resourceLatch.Setup(x => x.Rent())
                .Callback(() => latchState = true)
                .Returns(() => Disposable.Create(() => latchState = false));

            Container.Register(() => eventSource.Object);
            Container.Register(() => resourceLatch.Object);
        }

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void ShouldSuppressKeyboardIfBusy(bool isInjected, bool expected)
        {
            //Given
            var instance = CreateInstance();
            using var block = instance.Block(UserInputBlockType.All);

            //When
            var args = new KeyEventArgsExt(Keys.A, 0, 0, isKeyDown: true, isKeyUp: false, isExtendedKey: false, isInjected: isInjected);
            whenKey.OnNext(args);

            //Then
            args.Handled.ShouldBe(expected);
        }
        
        [Test]
        [TestCase(MouseButtons.Left, true, false, Keys.None, false, true)]
        [TestCase(MouseButtons.Left, true, true, Keys.None, false, true)]
        [TestCase(MouseButtons.Left, false, true, Keys.None, false, false)]
        public void ShouldSuppressMouseIfBusy(MouseButtons buttons, bool sendDown, bool sendUp, Keys modifiers, bool isInjected, bool expected)
        {
            //Given
            var instance = CreateInstance();
            using var block = instance.Block(UserInputBlockType.All);
            MouseEventExtArgs down = sendDown ? new MouseEventExtArgs(MouseButtons.Left, 1, new Point(), 0, 0, isMouseButtonDown: true, isMouseButtonUp: false, modifiers, isInjected) : default;
            MouseEventExtArgs up = sendUp ? new MouseEventExtArgs(MouseButtons.Left, 1, new Point(), 0, 0, isMouseButtonDown: false, isMouseButtonUp: true, modifiers, isInjected) : default;

            //When
            if (down != null)
            {
                whenMouse.OnNext(down);
            }

            if (up != null)
            {
                whenMouse.OnNext(up);
            }

            //Then
            down?.Handled.ShouldBe(expected);
            up?.Handled.ShouldBe(expected);
        }

        /// <summary>
        /// If mod is pressed BEFORE key they both must be suppressed
        /// </summary>
        [Test]
        public void ShouldHandleModifierIfPressedBeforeKey()
        {
            //Given
            var instance = CreateInstance();
            using var block = instance.Block(UserInputBlockType.All);

            var modDown = new KeyEventArgsExt(Keys.Control, isKeyDown: true);
            var keyDown = new KeyEventArgsExt(Keys.A, isKeyDown: true);
            
            //When
            whenKey.OnNext(modDown);
            whenKey.OnNext(keyDown);

            //Then
            modDown.Handled.ShouldBe(true);
            keyDown.Handled.ShouldBe(true);
        }

        [Test]
        public void ShouldHandleSimultaneousModAndKey()
        {
            //Given
            var instance = CreateInstance();
            using var block = instance.Block(UserInputBlockType.All);
            var keyDown = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: true);

            //When
            whenKey.OnNext(keyDown);

            //Then
            keyDown.Handled.ShouldBe(true);
        }

        
        /// <summary>
        ///   Ctrl+A pressed as a Hotkey, this triggers Block => A released (keyUp for Ctrl+A) => Ctrl released (keyUp for Control).
        ///   neither A nor Ctrl must be suppressed because blocker did not see initial events
        /// </summary>
        [Test]
        public void ShouldProcessModUpSeparately()
        {
            //Given
            var instance = CreateInstance();
            
            var modDown = new KeyEventArgsExt(Keys.Control, isKeyDown: true);
            var keyDown = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: true);
            var keyUp = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: false);
            var modUp = new KeyEventArgsExt(Keys.Control, isKeyDown: false);

            //When
            whenKey.OnNext(modDown);
            whenKey.OnNext(keyDown);
            using var block = instance.Block(UserInputBlockType.All);
            whenKey.OnNext(keyUp);
            whenKey.OnNext(modUp);


            //Then
            modDown.Handled.ShouldBe(false);
            keyDown.Handled.ShouldBe(false);
            keyUp.Handled.ShouldBe(false);
            modUp.Handled.ShouldBe(false);
        }
        
        /// <summary>
        ///   Ctrl => Block => Ctrl+A  => A released (keyUp for Ctrl+A) => Ctrl released (keyUp for Control).
        ///   Only Ctrl+A part must be suppressed, Ctrl must stay in the same state
        /// </summary>
        [Test]
        public void ShouldHandleIfBlockedBeforeKeyWithMod()
        {
            //Given
            var instance = CreateInstance();
            
            var modDown = new KeyEventArgsExt(Keys.Control, isKeyDown: true);
            var keyDown = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: true);
            var keyUp = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: false);
            var modUp = new KeyEventArgsExt(Keys.Control, isKeyDown: false);

            //When
            whenKey.OnNext(modDown);
            using var block = instance.Block(UserInputBlockType.All);
            whenKey.OnNext(keyDown);
            whenKey.OnNext(keyUp);
            whenKey.OnNext(modUp);

            //Then
            modDown.Handled.ShouldBe(false);
            keyDown.Handled.ShouldBe(true);
            keyUp.Handled.ShouldBe(true);
            modUp.Handled.ShouldBe(false);
        }
        
        /// <summary>
        ///   Block => Ctrl => Ctrl+A  => A released (keyUp for Ctrl+A) => Ctrl released (keyUp for Control).
        ///   Ctrl, Ctrl+A (it should not be possible to generate it via physical input because Ctrl is already suppressed) => A release => Ctrl released
        /// </summary>
        [Test]
        public void ShouldHandleIfBlockedBeforeKeys()
        {
            //Given
            var instance = CreateInstance();
            
            var modDown = new KeyEventArgsExt(Keys.Control, isKeyDown: true);
            var keyDown = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: true);
            var keyUp = new KeyEventArgsExt(Keys.Control | Keys.A, isKeyDown: false);
            var modUp = new KeyEventArgsExt(Keys.Control, isKeyDown: true);

            //When
            using var block = instance.Block(UserInputBlockType.All);
            whenKey.OnNext(modDown);
            whenKey.OnNext(keyDown);
            whenKey.OnNext(keyUp);
            whenKey.OnNext(modUp);

            //Then
            modDown.Handled.ShouldBe(true);
            keyDown.Handled.ShouldBe(true);
            keyUp.Handled.ShouldBe(true);
            modUp.Handled.ShouldBe(true);
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

        private UserInputBlocker CreateInstance()
        {
            return Container.Build<UserInputBlocker>().Create();
        }
    }
}