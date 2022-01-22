namespace PoeShared.UI;

public interface IErrorMonitorViewModel
{
    CommandWrapper ReportProblemCommand { get; }
    CommandWrapper ThrowExceptionCommand { get; }
}