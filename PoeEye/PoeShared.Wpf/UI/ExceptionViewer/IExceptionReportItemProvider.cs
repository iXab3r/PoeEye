using System.Collections.Generic;
using System.IO;

namespace PoeShared.UI
{
    public interface IExceptionReportItemProvider
    {
        IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory);
    }
}