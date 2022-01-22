using System.Windows.Forms;
using System.Windows.Input;
using AutoFixture;
using NUnit.Framework;
using PoeShared.Services;
using PoeShared.UI;
using Shouldly;
using WindowsHook;

namespace PoeShared.Tests.Services;

public class UserInputFilterConfiguratorFixture : FixtureBase
{
    protected override void SetUp()
    {
        base.SetUp();
    }

    [Test]
    public void ShouldTrackKeyState()
    {
        //Given
        var instance = CreateInstance();

        //When
        using var anchor = instance.AddToWhitelist(new HotkeyGesture(Key.A));

        //Then
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A)).ShouldBe(false);
    }

    [Test]
    public void ShouldTrackMultipleKeys()
    {
        //Given
        var instance = CreateInstance();

        //When
        using var anchor1 = instance.AddToWhitelist(new HotkeyGesture(Key.A));
        using var anchor2 = instance.AddToWhitelist(new HotkeyGesture(Key.B));

        //Then
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A)).ShouldBe(false);
        instance.ShouldProcess(new KeyEventArgsExt(Keys.B)).ShouldBe(false);
        instance.ShouldProcess(new KeyEventArgsExt(Keys.C)).ShouldBe(true);
    }

    [Test]
    public void ShouldSupportModifiers()
    {
        //Given
        var instance = CreateInstance();

        //When
        using var anchor1 = instance.AddToWhitelist(new HotkeyGesture(Key.A, ModifierKeys.Control));

        //Then
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A | Keys.Control)).ShouldBe(false);
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A)).ShouldBe(false);
    }

    [Test]
    public void ShouldIgnoreModifiers()
    {
        //Given
        var instance = CreateInstance();

        //When
        using var anchor1 = instance.AddToWhitelist(new HotkeyGesture(Key.A, ModifierKeys.Control));
        using var anchor2 = instance.AddToWhitelist(new HotkeyGesture(Key.A, ModifierKeys.Shift));

        //Then
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A | Keys.Alt)).ShouldBe(false);
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A | Keys.Shift)).ShouldBe(false);
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A | Keys.Control)).ShouldBe(false);
        instance.ShouldProcess(new KeyEventArgsExt(Keys.A)).ShouldBe(false);
    }

    private UserInputFilterConfigurator CreateInstance()
    {
        return Container.Create<UserInputFilterConfigurator>();
    }
}