using DynamicData;
using PoeShared.Reporting;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

internal interface IReportItemsAggregator : IDisposableReactiveObject
{
    IObservableList<ExceptionReportItem> ReportItems { get; }
    bool IsReady { get; }
    string Status { get; }
}