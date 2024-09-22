namespace PoeShared.Reporting;

public interface IErrorReportingService 
{
    void ReportProblem();

    void SetReportConsumer(IErrorReportHandler reportHandler);

    IDisposable AddReportItemProvider(IErrorReportItemProvider reportItemProvider);

    IDisposable AddExceptionInterceptor(IExceptionInterceptor exceptionInterceptor);
}