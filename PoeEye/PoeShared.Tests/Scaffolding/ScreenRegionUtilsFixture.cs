﻿using NUnit.Framework;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PoeShared.Native;
using PoeShared.Tests.Helpers;
using Shouldly;

using WinRect = System.Drawing.Rectangle;
using WinSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;
using WpfRect = System.Windows.Rect;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class ScreenRegionUtilsFixture : FixtureBase
{
    public static IEnumerable<NamedTestCaseData> ShouldCalculateProjectionCases()
    {
        yield return new NamedTestCaseData(
            new WpfRect(0, 0, 0, 0),
            new WpfSize(0, 0),
            new WinSize(0, 0),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "All empty" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(1, 1, 1, 1),
            new WpfSize(0, 0),
            new WinSize(1, 1),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Empty selector size" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(0, 0, 0, 0),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Empty selection" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(1, 1, 1, 1),
            new WpfSize(1, 1),
            new WinSize(0, 0),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Empty target bounds" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(0, 0, 1, 1),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 1, 1)
        ); 
            
        yield return new NamedTestCaseData(
            new WpfRect(0.49, 0.49, 0.49, 0.49),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 1, 1)
        );

        yield return new NamedTestCaseData(
            new WpfRect(0, 0, 1, 1),
            new WpfSize(1, 1),
            new WinSize(2, 2),
            new WinRect(0, 0, 2, 2)
        ); 

        yield return new NamedTestCaseData(
            new WpfRect(1, 1, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Selection outside by size client and selector bounds" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(0.5, 0.5, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 1, 1)
        ); 
            
        yield return new NamedTestCaseData(
            new WpfRect(0, 0, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 1, 1)
        ) { TestName = "Selection outside by size" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(2, 0, 1, 1),
            new WpfSize(2, 2),
            new WinSize(2, 2),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Selection outside bounds positive X" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(0, 2, 1, 1),
            new WpfSize(2, 2),
            new WinSize(2, 2),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Selection outside bounds positive Y" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(-1, 0, 1, 1),
            new WpfSize(2, 2),
            new WinSize(2, 2),
            new WinRect(0, 0, 0, 0)
        ) { TestName = "Selection outside bounds negative" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(0, 0, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WinRect(0, 0, 1, 1)
        ) { TestName = "Selection outside by size" }; 
            
        yield return new NamedTestCaseData(
            new WpfRect(1.2, 1.3, 3, 2),
            new WpfSize(4, 5),
            new WinSize(4, 5),
            new WinRect(1, 1, 3, 2)
        ); 
        
        yield return new NamedTestCaseData(
            new WpfRect(0.3, 0.4, 1.4, 1.25),
            new WpfSize(10, 10),
            new WinSize(10, 10),
            new WinRect(0, 0, 2, 2)
        ); 
    }

     public static IEnumerable<NamedTestCaseData> ShouldCalculateProjectionReverseCases()
    {
        yield return new NamedTestCaseData(
            new WinRect(0, 0, 0, 0),
            new WpfSize(0, 0),
            new WinSize(0, 0),
            new WpfRect(0, 0, 0, 0)
        ) { TestName = "All empty" };

        yield return new NamedTestCaseData(
            new WinRect(0, 0, 0, 0),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WpfRect(0, 0, 0, 0)
        ) { TestName = "Empty selection" };
        
        yield return new NamedTestCaseData(
            new WinRect(1, 1, 1, 1),
            new WpfSize(0, 0),
            new WinSize(1, 1),
            new WpfRect(0, 0, 0, 0)
        ) { TestName = "Empty selector size" };

        yield return new NamedTestCaseData(
            new WinRect(1, 1, 1, 1),
            new WpfSize(1, 1),
            new WinSize(0, 0),
            new WpfRect(0, 0, 0, 0)
        ) { TestName = "Empty target bounds" }; 
            
        yield return new NamedTestCaseData(
            new WinRect(0, 0, 1, 1),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WpfRect(0, 0, 1, 1)
        ){ TestName = "Default selection" }; 

        yield return new NamedTestCaseData(
            new WinRect(0, 0, 2, 2),
            new WpfSize(1, 1),
            new WinSize(2, 2),
            new WpfRect(0, 0, 1, 1)
        ); 

        yield return new NamedTestCaseData(
            new WinRect(1, 1, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WpfRect(1, 1, 0, 0)
        ) { TestName = "Selection outside by size client and selector bounds" }; 
            
        yield return new NamedTestCaseData(
            new WinRect(0, 0, 1, 1),
            new WpfSize(1, 1),
            new WinSize(2, 2),
            new WpfRect(0, 0, 0.5, 0.5)
        ){ TestName = "Selection in the middle" };  
            
        yield return new NamedTestCaseData(
            new WinRect(0, 0, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WpfRect(0, 0, 1, 1)
        ) { TestName = "Selection outside by size" }; 
            
        yield return new NamedTestCaseData(
            new WinRect(2, 0, 1, 1),
            new WpfSize(2, 2),
            new WinSize(2, 2),
            new WpfRect(2, 0, 0, 1)
        ) { TestName = "Selection outside bounds positive X" }; 
            
        yield return new NamedTestCaseData(
            new WinRect(0, 2, 1, 1),
            new WpfSize(2, 2),
            new WinSize(2, 2),
            new WpfRect(0, 2, 1, 0)
        ) { TestName = "Selection outside bounds positive Y" }; 
            
        yield return new NamedTestCaseData(
            new WinRect(-1, 0, 1, 1),
            new WpfSize(2, 2),
            new WinSize(2, 2),
            new WpfRect(0, 0, 0, 1)
        ) { TestName = "Selection outside bounds negative" }; 
            
        yield return new NamedTestCaseData(
            new WinRect(0, 0, 2, 2),
            new WpfSize(1, 1),
            new WinSize(1, 1),
            new WpfRect(0, 0, 1, 1)
        ) { TestName = "Selection outside by size" }; 
        
        yield return new NamedTestCaseData(
            new WinRect(0, 0, 2, 2),
            new WpfSize(10, 10),
            new WinSize(10, 10),
            new WpfRect(0, 0, 2, 2)
        ); 
    }
    
    [Test]
    [TestCaseSource(nameof(ShouldCalculateProjectionCases))]
    public void ShouldCalculateProjection(WpfRect selection, WpfSize selector, WinSize clientSize, WinRect expected)
    {
        //Given
        Log.Debug($"{new {selection, selector, clientSize, expected}}");

        //When
        var result = ScreenRegionUtils.CalculateProjection(selection, selector, clientSize);

        //Then
        result.ShouldBe(expected);
    }
    
    [Test]
    [TestCaseSource(nameof(ShouldCalculateProjectionReverseCases))]
    public void ShouldCalculateProjectionReverse(WinRect projected, WpfSize selector, WinSize clientSize, WpfRect expected)
    {
        //Given
        Log.Debug($"{new {projected, selector, clientSize, expected}}");

        //When
        var result = ScreenRegionUtils.ReverseProjection(projected, selector, clientSize);

        //Then
        result.X.ShouldBe(expected.X, 1e-1);
        result.Y.ShouldBe(expected.Y, 1e-1);
        result.Width.ShouldBe(expected.Width, 1e-1);
        result.Height.ShouldBe(expected.Height, 1e-1);
    }

    public static IEnumerable<NamedTestCaseData> ShouldCalculateBoundsCases()
    {
        yield return new NamedTestCaseData(
            new Rectangle(0, 0, 0, 0),
            new Size(0, 0),
            new Rectangle(0, 0, 0, 0),
            new Rectangle(0, 0, 0, 0)
        ) { TestName = "All empty" }; 
            
        yield return new NamedTestCaseData(
            new Rectangle(1, 1, 1, 1),
            new Size(0, 0),
            new Rectangle(1, 1, 1, 1),
            new Rectangle(0, 0, 0, 0)
        ) { TestName = "Empty selector size" }; 
            
        yield return new NamedTestCaseData(
            new Rectangle(0, 0, 0, 0),
            new Size(1, 1),
            new Rectangle(1, 1, 1, 1),
            new Rectangle(0, 0, 0, 0)
        ) { TestName = "Empty selection" }; 
            
        yield return new NamedTestCaseData(
            new Rectangle(1, 1, 1, 1),
            new Size(1, 1),
            new Rectangle(0, 0, 0, 0),
            new Rectangle(0, 0, 0, 0)
        ) { TestName = "Empty target bounds" }; 
            
        /*
         These tests should be carefully re-evaluated
        yield return new NamedTestCaseData(
            new Rectangle(1, 1, 1, 1),
            new Size(1, 1),
            new Rectangle(1, 1, 1, 1),
            new Rectangle(1, 1, 1, 1)
        ) { TestName = "1px" }; 
            
        yield return new NamedTestCaseData(
            new Rectangle(1, 1, 2, 2),
            new Size(1, 1),
            new Rectangle(1, 1, 1, 1),
            new Rectangle(1, 1, 1, 1)
        ) { TestName = "Selection outside by size client and selector bounds" }; 
        */
    }

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
                    
                srcPoint.ShouldBe(dstPoint, $"Source: {srcPoint}, Converted: {converted}, Result: {dstPoint}");
            }
        }
    }
}