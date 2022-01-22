using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class BinaryExtensionsFixture
{
    [Test]
    [TestCase(0b0000_0000, 0, 0, 0b0000_0000)]
    [TestCase(0b0000_0001, 0, 1, 1)]
    [TestCase(0b0000_0010, 1, 1, 1)]
    [TestCase(0b0000_0100, 2, 1, 1)]
    [TestCase(0b0000_1000, 3, 1, 1)]
    [TestCase(0b0001_0000, 4, 1, 1)]
    [TestCase(0b0010_0000, 5, 1, 1)]
    [TestCase(0b0100_0000, 6, 1, 1)]
    [TestCase(0b1000_0000, 7, 1, 1)]
    [TestCase(0b1111_1110, 0, 2, 0b0000_0010)]
    [TestCase(0b1111_1011, 2, 2, 0b0000_0010)]
    [TestCase(0b1111_1010, 0, 4, 0b0000_1010)]
    [TestCase(0b1010_1111, 4, 4, 0b0000_1010)]
    public void ShouldGetBitsFromByte(byte input, byte start, byte length, byte expected)
    {
        //Given
        //When
        var result = input.GetBits(start, length);

        //Then
        result.ShouldBe(expected);
    }
        
    [Test]
    [TestCase(0U, 0, 0, 0U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 0, 0, 0U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 0, 16, 0b0000_0000_0000_0000_1111_1010_0111_1100U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 16, 16, 0b0000_0000_0000_0000_1111_1110_0010_1100U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 0, 32, 0b1111_1110_0010_1100_1111_1010_0111_1100U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 0, 4, 0b0000_0000_0000_0000_0000_0000_0000_1100U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 0, 8, 0b0000_0000_0000_0000_0000_0000_0111_1100U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 4, 8, 0b0000_0000_0000_0000_0000_0000_1010_0111U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 28, 4, 0b0000_0000_0000_0000_0000_0000_0000_1111U)]
    [TestCase(0b1111_1110_0010_1100_1111_1010_0111_1100U, 24, 8, 0b0000_0000_0000_0000_0000_0000_1111_1110U)]
    public void ShouldGetBitsFromInt(uint input, byte start, byte length, uint expected)
    {
        //Given
        //When
        var result = input.GetBits(start, length);

        //Then
        result.ShouldBe(expected);
    }
}