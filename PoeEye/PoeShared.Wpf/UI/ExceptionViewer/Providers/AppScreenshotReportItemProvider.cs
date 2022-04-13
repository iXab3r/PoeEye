using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.WindowSeekers;

namespace PoeShared.UI.Providers;

internal sealed class AppScreenshotReportItemProvider : IExceptionReportItemProvider
{
    private readonly IWindowSeeker windowSeeker;

    public AppScreenshotReportItemProvider(IWindowSeeker windowSeeker)
    {
        this.windowSeeker = windowSeeker;
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        const double minWindowArea = 100;
        windowSeeker.Refresh();
        var appWindows = windowSeeker.Windows
            .Where(x => x.ProcessId == Environment.ProcessId).ToArray();

        var visibleWindows = appWindows
            .Where(x => !string.IsNullOrEmpty(x.Title))
            .Select(x => new {Window = x, Area = x.WindowBounds.Size.Area(), CanScreenshot = x.IsVisible && !x.IsIconic})
            .Where(x => x.CanScreenshot && x.Area > minWindowArea)
            .OrderBy(x => x.Area)
            .ToArray();
        
        var screenshots = visibleWindows.SelectSafe(x => new
        {
            x.Window,
            Screenshot = UnsafeNative.GetWindowImageViaCopyFromScreen(x.Window.Handle, Rectangle.Empty)
        });

        foreach (var item in screenshots)
        {
            var destinationFileName = Path.Combine(outputDirectory.FullName, $"Wnd {item.Window.Title} {item.Screenshot.Width}x{item.Screenshot.Height} {item.Window.Handle.ToHexadecimal()}.png");
            item.Screenshot.Save(destinationFileName);
            item.Screenshot.Dispose();
            yield return new ExceptionReportItem()
            {
                Description = $"Window {item.Window.Title}",
                Attachment = new FileInfo(destinationFileName),
                Attached = true
            };
        }
    }
}