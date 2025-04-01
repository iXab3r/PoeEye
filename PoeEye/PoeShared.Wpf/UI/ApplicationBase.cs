using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using CommandLine;
using Unity;
using Unity.Lifetime;

namespace PoeShared.UI;

public abstract class ApplicationBase : Application
{
    private readonly ApplicationCore core;
    private readonly IUnityContainer container;
    
    protected ApplicationBase()
    {
        container = new UnityContainer();
        core = container.Resolve<ApplicationCore>();
        core.BindToApplication(this);
    }
}
