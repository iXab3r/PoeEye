﻿using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PropertyBinder;
using Unity;

namespace PoeShared.UI;

internal sealed class ErrorMonitorViewModel : DisposableReactiveObject, IErrorMonitorViewModel
{
    private static readonly Binder<ErrorMonitorViewModel> Binder = new();
    private readonly IExceptionReportingService exceptionReportingService;

    static ErrorMonitorViewModel()
    {
    }

    public ErrorMonitorViewModel(
        IAppArguments appArguments, 
        IExceptionReportingService exceptionReportingService,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        this.exceptionReportingService = exceptionReportingService;
        ReportProblemCommand = CommandWrapper.Create(ReportProblemCommandExecuted);
        ThrowExceptionCommand = appArguments.IsDebugMode ? CommandWrapper.Create(() => uiScheduler.Schedule(() => throw new ApplicationException("Exception thrown on UI scheduler"))) : default;
            
        Binder.Attach(this).AddTo(Anchors);
    }

    public ICommandWrapper ReportProblemCommand { get; }
        
    public ICommandWrapper ThrowExceptionCommand { get; }

    private async Task ReportProblemCommandExecuted()
    {
        await Task.Run(() => exceptionReportingService.ReportProblem());
    }
}