using System.Collections.Generic;
using System.IO;
using PoeShared.Native;

namespace PoeShared.UI.Providers
{
    internal sealed class DesktopScreenshotReportItemProvider : IExceptionReportItemProvider
    {
        public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
        {
            var screen = System.Windows.Forms.SystemInformation.VirtualScreen;
            var destinationFileName = Path.Combine(outputDirectory.FullName, $"Screen {screen.Width}x{screen.Height}.png");
            var image = UnsafeNative.GetDesktopImageViaCopyFromScreen(screen);
            image.Save(destinationFileName);
            yield return new ExceptionReportItem()
            {
                Description = $"Desktop screenshot",
                Attachment = new FileInfo(destinationFileName),
                Attached = false
            };
        }
    }
}