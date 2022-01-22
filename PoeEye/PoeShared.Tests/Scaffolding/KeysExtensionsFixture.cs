using System.Windows.Forms;
using System.Windows.Input;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class KeyGestureExtensions
{
    [Test]
    [TestCase(Keys.None, ModifierKeys.None)]
    [TestCase(Keys.A, ModifierKeys.None)]
    [TestCase(Keys.Alt, ModifierKeys.Alt)]
    [TestCase(Keys.Control, ModifierKeys.Control)]
    [TestCase(Keys.Shift, ModifierKeys.Shift)]
    [TestCase(Keys.LWin, ModifierKeys.Windows)]
    [TestCase(Keys.RWin, ModifierKeys.Windows)]
    [TestCase(Keys.Shift | Keys.Control, ModifierKeys.Shift | ModifierKeys.Control)]
    [TestCase(Keys.Alt | Keys.Control, ModifierKeys.Alt | ModifierKeys.Control)]
    [TestCase(Keys.Shift | Keys.Control | Keys.Alt, ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt)]
    [TestCase(Keys.Shift | Keys.Control | Keys.Alt | Keys.LWin, ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)]
    [TestCase(Keys.Shift | Keys.Control | Keys.Alt | Keys.RWin, ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)]
    public void ShouldGetModifierKeys(Keys keys, ModifierKeys expected)
    {
        //Given
        //When
        var result = keys.ToModifiers();

        //Then
        result.ShouldBe(expected);
    }
}