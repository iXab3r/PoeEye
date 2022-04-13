using System;
using System.Threading.Tasks;

namespace PoeShared.UI;

public interface IExceptionReportingService 
{
    void ReportProblem();

    void SetReportConsumer(IExceptionReportHandler reportHandler);

    IDisposable AddReportItemProvider(IExceptionReportItemProvider reportItemProvider);
}