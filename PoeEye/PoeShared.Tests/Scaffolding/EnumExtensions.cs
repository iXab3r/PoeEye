using System.Windows.Forms;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding
{
    [TestFixture]
    public class EnumExtensions
    {
        [Test]
        [TestCase(Keys.None, Keys.None, Keys.None)]
        [TestCase(Keys.A, Keys.None, Keys.A)]
        [TestCase(Keys.A, Keys.B, Keys.A)]
        [TestCase(Keys.A, Keys.Shift, Keys.A)]
        [TestCase(Keys.A, Keys.Control, Keys.A)]
        [TestCase(Keys.A, Keys.Alt, Keys.A)]
        [TestCase(Keys.A | Keys.Shift, Keys.Alt, Keys.A | Keys.Shift)]
        [TestCase(Keys.A | Keys.Control, Keys.Control, Keys.A)]
        [TestCase(Keys.A | Keys.Alt, Keys.Alt, Keys.A)]
        public void ShouldRemoveFlag(Keys input, Keys toRemove, Keys expected)
        {
            //Given
            //When
            var result = input.RemoveFlag(toRemove);

            //Then
            result.ShouldBe(expected);
        }
        
        [Test]
        [TestCase(Keys.None, Keys.None, Keys.None)]
        [TestCase(Keys.A, Keys.A, Keys.None)]
        [TestCase(Keys.A | Keys.Shift, Keys.A, Keys.Shift)]
        [TestCase(Keys.A | Keys.Control, Keys.A, Keys.Control)]
        [TestCase(Keys.A | Keys.Alt, Keys.A, Keys.Alt)]
        [TestCase(Keys.A | Keys.Alt | Keys.Shift | Keys.Control, Keys.A | Keys.Alt | Keys.Shift, Keys.Control)]
        [TestCase(Keys.A | Keys.Alt | Keys.Shift | Keys.Control, Keys.A, Keys.Control | Keys.Shift | Keys.Alt)]
        [TestCase(Keys.A | Keys.Alt | Keys.Shift | Keys.Control, Keys.A, Keys.Control, Keys.Shift, Keys.Alt)]
        public void ShouldRemoveFlags(Keys input, Keys expected, params Keys[] toRemove)
        {
            //Given
            //When
            var result = input.RemoveFlag(toRemove);

            //Then
            result.ShouldBe(expected);
        }
    }
}