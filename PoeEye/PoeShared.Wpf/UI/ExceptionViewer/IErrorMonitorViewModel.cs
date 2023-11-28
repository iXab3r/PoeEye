using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface IErrorMonitorViewModel
{
    ICommandWrapper ReportProblemCommand { get; }
    ICommandWrapper ThrowExceptionCommand { get; }
}