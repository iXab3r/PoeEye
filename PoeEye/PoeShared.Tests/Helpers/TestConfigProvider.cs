using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Tests.Helpers;

internal sealed class TestConfigProvider<T> : DisposableReactiveObject, IConfigProvider<T> where T : IPoeEyeConfig, new()
{
    public T ActualConfig { get; private set; }
    
    public IObservable<T> WhenChanged { get; }

    public TestConfigProvider()
    {
        ActualConfig = new T();
        WhenChanged = this.WhenAnyValue(x => x.ActualConfig);
    }

    public void Reload()
    {
    }

    public void Save(T config)
    {
        ActualConfig = config;
    }

    public IObservable<T1> ListenTo<T1>(Expression<Func<T, T1>> fieldToMonitor)
    {
        var functor = fieldToMonitor.Compile();
        return
            WhenChanged
                .Select(config => functor(config))
                .DistinctUntilChanged();
    }
}