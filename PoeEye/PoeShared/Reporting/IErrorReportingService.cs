namespace PoeShared.Reporting;

/// <summary>
/// Opens user-reviewed problem reports and manages the host's report attachments and consumer.
/// </summary>
public interface IErrorReportingService 
{
    /// <summary>
    /// Opens a problem report with the host defaults and an optional exception.
    /// </summary>
    void ReportProblem(Exception error = null);

    /// <summary>
    /// Opens a problem report with caller-provided values and host defaults for omitted values.
    /// The initial comment remains editable in the dialog.
    /// </summary>
    void ReportProblem(PoeShared.UI.ExceptionDialogConfig config);

    void SetReportConsumer(IErrorReportHandler reportHandler);

    IDisposable AddReportItemProvider(IErrorReportItemProvider reportItemProvider);

    IDisposable AddExceptionInterceptor(IExceptionInterceptor exceptionInterceptor);
}
