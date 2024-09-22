namespace PoeShared.Reporting;

public interface IErrorReportItemProvider
{
    IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory);
}