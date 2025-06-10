namespace PoeShared.Reporting;

public interface IErrorReportingService 
{
    void ReportProblem(Exception error = null);

    void SetReportConsumer(IErrorReportHandler reportHandler);

    IDisposable AddReportItemProvider(IErrorReportItemProvider reportItemProvider);

    IDisposable AddExceptionInterceptor(IExceptionInterceptor exceptionInterceptor);
}