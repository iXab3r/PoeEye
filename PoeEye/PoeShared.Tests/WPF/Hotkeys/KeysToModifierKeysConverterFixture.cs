using System.Windows.Forms;
using System.Windows.Input;
using NUnit.Framework;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys;

[TestFixture]
public class KeysToModifierKeysConverterFixture
{
    [Test]
    [TestCase(Keys.None, ModifierKeys.None)]
    [TestCase(Keys.LControlKey, ModifierKeys.None)]
    [TestCase(Keys.RControlKey, ModifierKeys.None)]
    [TestCase(Keys.LShiftKey, ModifierKeys.None)]
    [TestCase(Keys.RShiftKey, ModifierKeys.None)]
    [TestCase(Keys.LMenu, ModifierKeys.None)]
    [TestCase(Keys.RMenu, ModifierKeys.None)]
        
    [TestCase(Keys.Shift, ModifierKeys.Shift)]
    [TestCase(Keys.Control, ModifierKeys.Control)]
    [TestCase(Keys.Alt, ModifierKeys.Alt)]
    [TestCase(Keys.LWin, ModifierKeys.Windows)]
    [TestCase(Keys.RWin, ModifierKeys.Windows)]
    public void ShouldConvert(Keys input, ModifierKeys expected)
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Convert(input);

        //Then
        result.ShouldBe(expected);
    }

    private KeysToModifierKeysConverter CreateInstance()
    {
        return new();
    }
}