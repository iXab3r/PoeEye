using System.Reactive;
using PoeShared.Common;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class EnablementControllerExtensions
{
    public static IDisposable EnableIf(this IEnablementController instance, IObservable<AnnotatedBoolean> condition)
    {
        return condition
            .Select(x => x.Value ? Observable.Empty<Unit>() : instance.Disable(x.Annotation).ToObservable())
            .Switch()
            .Subscribe();
    }
    
    public static IDisposable EnableIf(this IEnablementController instance, IObservable<IEnablementController> parentControllerSource, string parentName)
    {
        var condition = parentControllerSource
            .Select(controller => controller != null
                ? controller.WhenAnyValue(x => x.IsEnabledState).Select(x => new AnnotatedBoolean(x.Value, $"{parentName} -> {x.Annotation}"))
                : Observable.Return(new AnnotatedBoolean(true, $"{parentName} -> not set")))
            .Switch();

        return instance.EnableIf(condition);
    }
    
    public static IDisposable EnableIf(this IEnablementController instance, IEnablementController parentController, string parentName)
    {
        return EnableIf(instance, Observable.Return(parentController), parentName);
    }
}