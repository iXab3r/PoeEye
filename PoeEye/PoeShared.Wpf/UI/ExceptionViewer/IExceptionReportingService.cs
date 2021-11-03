using System;
using System.Threading.Tasks;

namespace PoeShared.UI
{
    public interface IExceptionReportingService 
    {
        Task<ExceptionDialogConfig> PrepareConfig();

        void SetReportConsumer(IExceptionReportHandler reportHandler);

        IDisposable AddReportItemProvider(IExceptionReportItemProvider reportItemProvider);
    }
}