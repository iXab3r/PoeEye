using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoeShared.UI
{
    public interface IExceptionReportingService
    {
        Task<ExceptionDialogConfig> PrepareConfig();
        
        Task<IReadOnlyList<ExceptionReportItem>> PrepareReportItems(Exception exception);
    }
}