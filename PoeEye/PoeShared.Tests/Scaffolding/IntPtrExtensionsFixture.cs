using System;
using NUnit.Framework;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class IntPtrExtensionsFixture
{
    [Test]
    [TestCase(-900, 240)]
    [TestCase(1024, -320)]
    [TestCase(-1100, -200)]
    public void ShouldDecodeSignedWordsFromPackedLParam(int lowWord, int highWord)
    {
        // Given
        var value = UnsafeNative.MakeLParam(lowWord, highWord);

        // When
        var decodedLowWord = value.SignedLoWord();
        var decodedHighWord = value.SignedHiWord();

        // Then
        decodedLowWord.ShouldBe((short)lowWord);
        decodedHighWord.ShouldBe((short)highWord);
    }
}
