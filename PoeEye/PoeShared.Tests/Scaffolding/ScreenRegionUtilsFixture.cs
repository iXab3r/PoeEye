using NUnit.Framework;
using AutoFixture;
using System;
using System.Drawing;
using System.Windows.Forms;
using PoeShared.Native;
using Shouldly;

namespace PoeShared.Tests.Scaffolding
{
    [TestFixture]
    public class ScreenRegionUtilsFixture
    {
        [Test]
        [TestCase(0, 0, 3440, 1440, 0, 0, 3440, 1440)]
        [TestCase(0, 0, 3440, 1440, 0, -1080, 3440, 2520)]
        //[TestCase(-1920, -1080, 3440, 1440, 0, -1080, 3440, 2520)] FIXME: Breaks on range values #EA-102
        public void ShouldConvertWinInputToScreenAndViceVersa(int minX, int minY, int maxX, int maxY, int screenX, int screenY, int screenWidth, int screenHeight)
        {
            var screenBounds = SystemInformation.VirtualScreen;
            var bounds = new Rectangle(screenX, screenY, screenWidth, screenHeight);
            for (var y = minX; y < maxX; y++)
            {
                for (var x = minY; x < maxY; x++)
                {
                    var srcPoint = new Point(x, y);
                    var converted = ScreenRegionUtils.ToWinInputCoordinates(srcPoint, bounds);
                    var dstPoint = ScreenRegionUtils.ToScreenCoordinates(converted.X, converted.Y, bounds);
                    
                    srcPoint.ShouldBe(dstPoint, () => $"Source: {srcPoint}, Converted: {converted}, Result: {dstPoint}");
                }
            }
        }
    }
}