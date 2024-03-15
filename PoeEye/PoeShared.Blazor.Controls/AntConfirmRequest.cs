using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AntDesign;

namespace PoeShared.Blazor.Controls;

public sealed record AntConfirmRequest 
{
    private readonly ISubject<bool> resultSink = new Subject<bool>();

    public required ConfirmOptions Options { get; init; }
    
    public IObservable<bool> Result { get; }

    public AntConfirmRequest()
    {
        Result = resultSink.Take(1);
    }

    public void ReportResult(bool result)
    {
        resultSink.OnNext(result);
    }
}