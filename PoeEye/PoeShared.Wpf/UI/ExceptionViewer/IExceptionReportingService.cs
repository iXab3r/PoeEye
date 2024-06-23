using System;

namespace PoeShared.UI;

public interface IExceptionReportingService 
{
    void ReportProblem();

    void SetReportConsumer(IExceptionReportHandler reportHandler);

    IDisposable AddReportItemProvider(IExceptionReportItemProvider reportItemProvider);

    IDisposable AddExceptionInterceptor(IExceptionInterceptor exceptionInterceptor);
}