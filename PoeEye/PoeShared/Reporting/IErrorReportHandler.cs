namespace PoeShared.Reporting;

public interface IErrorReportHandler
{
    Task<string> Handle(FileInfo exceptionReport);
}