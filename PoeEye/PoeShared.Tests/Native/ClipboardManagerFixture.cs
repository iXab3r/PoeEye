using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using PoeShared.Native;
using Shouldly;

namespace PoeShared.Tests.Native;

[TestFixture]
[Category("Integration")]
[Explicit("Run the test host on a private EA1394_* window station; never on the user's clipboard.")]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class ClipboardManagerFixture
{
    private ClipboardManager clipboard;

    [SetUp]
    public void SetUp()
    {
        var name = new StringBuilder(256);
        if (!GetUserObjectInformation(GetProcessWindowStation(), 2, name, 512, out _)
            || !name.ToString().StartsWith("EA1394_", StringComparison.Ordinal))
        {
            Assert.Ignore("A private EA1394_* window station is required to protect the user's clipboard.");
        }

        clipboard = new ClipboardManager(ImmediateScheduler.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (clipboard == null)
        {
            return;
        }

        Clipboard.Clear();
        clipboard.Dispose();
        clipboard = null;
    }

    /// <summary>
    /// WHAT: Image copy preserves pixels and survives disposal of the source and repeated reads.
    /// HOW: Publish a 77x23 image through the real service, dispose it, then compare every decoded pixel twice.
    /// </summary>
    [TestCase(PixelFormat.Format24bppRgb)]
    [TestCase(PixelFormat.Format32bppArgb)]
    [TestCase(PixelFormat.Format32bppPArgb)]
    [TestCase(PixelFormat.Format8bppIndexed)]
    public void ShouldPreserveImagePixelsAndLifetime(PixelFormat format)
    {
        // Given
        var expected = new int[77 * 23];
        using (var source = new Bitmap(77, 23, format))
        {
            if (format == PixelFormat.Format8bppIndexed)
            {
                var palette = source.Palette;
                palette.Entries[0] = Color.FromArgb(128, 200, 100, 50);
                source.Palette = palette;
            }
            else
            {
                for (var y = 0; y < source.Height; y++)
                for (var x = 0; x < source.Width; x++)
                {
                    source.SetPixel(x, y, Color.FromArgb(
                        format == PixelFormat.Format24bppRgb ? 255 : new[] { 0, 128, 255 }[x % 3], 200, 100, 50));
                }
            }

            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = source.GetPixel(i % 77, i / 77).ToArgb();
            }

            // When
            clipboard.SetImage(source);
        }

        // Then
        clipboard.ContainsImage().ShouldBeTrue();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            using var result = (Bitmap) clipboard.GetImage();
            result.Size.ShouldBe(new Size(77, 23));
            for (var i = 0; i < expected.Length; i++)
            {
                result.GetPixel(i % 77, i / 77).ToArgb().ShouldBe(expected[i]);
            }
        }
    }

    /// <summary>
    /// WHAT: Bitmap-only producers and consumers remain compatible, and a new image replaces the old one.
    /// HOW: Exchange distinct opaque images between the service and the legacy WinForms image API.
    /// </summary>
    [Test]
    public void ShouldExchangeImagesWithLegacyConsumers()
    {
        // Given
        using var oldImage = new Bitmap(9, 5);
        oldImage.SetPixel(0, 0, Color.Red);
        Clipboard.SetImage(oldImage);

        // When
        using var imported = (Bitmap) clipboard.GetImage();

        // Then
        clipboard.ContainsImage().ShouldBeTrue();
        imported.Size.ShouldBe(oldImage.Size);
        imported.GetPixel(0, 0).ToArgb().ShouldBe(Color.Red.ToArgb());

        // When
        using var newImage = new Bitmap(3, 2);
        newImage.SetPixel(0, 0, Color.Blue);
        clipboard.SetImage(newImage);

        // Then
        using var legacy = (Bitmap) Clipboard.GetImage();
        using var current = (Bitmap) clipboard.GetImage();
        legacy.Size.ShouldBe(newImage.Size);
        current.Size.ShouldBe(newImage.Size);
        legacy.GetPixel(0, 0).ToArgb().ShouldBe(Color.Blue.ToArgb());
        current.GetPixel(0, 0).ToArgb().ShouldBe(Color.Blue.ToArgb());
    }

    /// <summary>
    /// WHAT: PNG-only clipboard images are detected and read without taking ownership of the producer's stream.
    /// HOW: Publish retained and flushed PNG payloads, then read twice and verify pixels and producer lifetime.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ShouldReadPngOnlyPayload(bool flush)
    {
        // Given
        using var source = new Bitmap(2, 2);
        source.SetPixel(0, 0, Color.FromArgb(128, 200, 100, 50));
        using var stream = new MemoryStream();
        source.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        var data = new DataObject();
        data.SetData("PNG", false, stream);

        // When
        Clipboard.SetDataObject(data, flush);

        // Then
        clipboard.ContainsImage().ShouldBeTrue();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            using var result = (Bitmap) clipboard.GetImage();
            result.GetPixel(0, 0).ToArgb().ShouldBe(source.GetPixel(0, 0).ToArgb());
        }
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// WHAT: Image inspection leaves supported text and file-move clipboard payloads intact.
    /// HOW: Publish each payload, query image availability and read, then verify the original content.
    /// </summary>
    [Test]
    public void ShouldPreserveTextAndFilePayloadsDuringImageInspection()
    {
        // Given
        Clipboard.SetText("clipboard text control");

        // When
        var hasTextImage = clipboard.ContainsImage();
        using var textImage = clipboard.GetImage();

        // Then
        hasTextImage.ShouldBeFalse();
        textImage.ShouldBeNull();
        Clipboard.GetText().ShouldBe("clipboard text control");

        // Given
        var files = new DataObject();
        files.SetFileDropList(new StringCollection { @"C:\clipboard-control.txt" });
        using var effect = new MemoryStream(BitConverter.GetBytes(2));
        files.SetData("Preferred DropEffect", effect);
        Clipboard.SetDataObject(files, true);

        // When
        var hasFileImage = clipboard.ContainsImage();
        using var fileImage = clipboard.GetImage();

        // Then
        hasFileImage.ShouldBeFalse();
        fileImage.ShouldBeNull();
        Clipboard.GetFileDropList()[0].ShouldBe(@"C:\clipboard-control.txt");
        var storedEffect = (MemoryStream) Clipboard.GetDataObject().GetData("Preferred DropEffect", false);
        BitConverter.ToInt32(storedEffect.ToArray(), 0).ShouldBe(2);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetProcessWindowStation();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformation(IntPtr obj, int index, StringBuilder text, uint size, out uint needed);
}
