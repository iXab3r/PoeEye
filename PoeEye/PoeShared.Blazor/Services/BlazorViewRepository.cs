using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reflection;
using PoeShared.Scaffolding;
using PoeShared.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Kernel;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Logging;

namespace PoeShared.Blazor.Services;

public class BlazorViewRepository : DisposableReactiveObjectWithLogger, IBlazorViewRepository, IBlazorViewRegistrator
{
    private readonly IClock clock;
    private readonly ConcurrentQueue<Assembly> unprocessedAssemblies = new();
    private readonly SourceCache<ViewRegistration, ViewKey> viewsByKey = new(x => x.Key);
    private readonly ISubject<Unit> whenChanged = new Subject<Unit>();

    private readonly int maxProcessingThreads = Math.Min(4, Environment.ProcessorCount);

    public BlazorViewRepository(
        IClock clock,
        IAssemblyTracker assemblyTracker)
    {
        Log.AddSuffix("Blazor views cache");

        this.clock = clock;

        Log.Info($"Processing threads: {maxProcessingThreads} (processor count: {Environment.ProcessorCount})");
        this.WhenAnyValue(x => x.AutomaticallyProcessAssemblies)
            .Select(x => x ? assemblyTracker.Assemblies.WhenAdded : Observable.Empty<Assembly>())
            .Switch()
            .Subscribe(x =>
            {
                Log.Debug($"Adding assembly to processing queue: {x}, size: {unprocessedAssemblies.Count}");
                unprocessedAssemblies.Enqueue(x);
            }, Log.HandleUiException)
            .AddTo(Anchors);

        viewsByKey
            .Connect()
            .ToUnit()
            .Subscribe(whenChanged)
            .AddTo(Anchors);
    }

    public bool AutomaticallyProcessAssemblies { get; set; } = true;

    public IObservable<Unit> WhenChanged => whenChanged;

    public void RegisterViewType(Type viewType, object key = default)
    {
        EnsureQueueIsProcessed();
        var baseViewType = ResolveBaseViewType(viewType);
        var contentType = ResolveContentType(baseViewType);
        RegisterViewType(viewType, contentType, key);
    }

    private void RegisterViewType(Type viewType, Type dataContextType, object key)
    {
        var viewKey = new ViewKey()
        {
            DataContextType = dataContextType,
            Key = key
        };
        var log = Log.WithSuffix(viewKey.ToString());
        log.Debug($"Registering view type");

        var registration = new ViewRegistration()
        {
            RegistrationTimestamp = clock.Now,
            ViewType = viewType,
            Key = viewKey
        };

        viewsByKey.AddOrUpdate(viewKey, () =>
        {
            log.Debug($"Registered new view type: {registration}");
            return registration;
        }, (_, existingRegistration) =>
        {
            log.Debug($"Overriding registration with a new view type: {registration}, existing: {existingRegistration}");
            return registration;
        });
    }

    public Type ResolveViewType(Type contentType, object key = default)
    {
        var viewKey = new ViewKey()
        {
            DataContextType = contentType,
            Key = key
        };
        var log = Log.WithSuffix(viewKey.ToString());
        log.Debug($"Resolving view type");
        EnsureQueueIsProcessed();

        // resolve by content type
        if (TryResolveViewType(viewKey, out var registration))
        {
            log.Debug($"Resolved registered view by key {viewKey}: {registration}");
            return registration.ViewType;
        }

        {
            // resolve by interface - only direct interfaces are supported
            var interfaces = contentType.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                var byInterfaceKey = new ViewKey() { DataContextType = @interface, Key = key };
                if (TryResolveViewType(byInterfaceKey, out var registrationByInterface))
                {
                    log.Debug($"Resolved registered view by interface {byInterfaceKey}: {registrationByInterface}");
                    return registrationByInterface.ViewType;
                }
            }
        }

        return null;
    }

    private bool TryResolveViewType(ViewKey viewKey, out ViewRegistration registration)
    {
        if (viewsByKey.TryGetValue(viewKey, out registration))
        {
            return true;
        }

        registration = default;
        return false;
    }

    private void EnsureQueueIsProcessed()
    {
        EnsureQueueIsProcessedParallel();
    }

    private void EnsureQueueIsProcessedParallel()
    {
        var sw = ValueStopwatch.StartNew();
        var processedAssembliesCount = 0;
        var viewsRegistered = 0;
        ConcurrentQueueUtils.Process(unprocessedAssemblies, assembly =>
        {
            try
            {
                var hasViewsAttribute = assembly.GetCustomAttribute<AssemblyHasBlazorViewsAttribute>();
                if (hasViewsAttribute == null)
                {
                    return;
                }

                Log.Info($"Loading BlazorViews from assembly {assembly}");
                var views = LoadViewsFromAssembly(assembly);
                Interlocked.Increment(ref processedAssembliesCount);
                Interlocked.Add(ref viewsRegistered, views);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to process the assembly: {assembly}", e);
            }
        });
        if (viewsRegistered <= 0)
        {
            return;
        }
        Log.Info($"Processed assemblies({processedAssembliesCount}), loaded {viewsRegistered} view(s) in {sw.ElapsedMilliseconds:F0}ms");
    }

    private int LoadViewsFromAssembly(Assembly assembly)
    {
        var logger = Log.WithSuffix(assembly.ToString());
        logger.Debug("Loading Blazor views from assembly");

        var viewsRegistered = 0;
        
        try
        {
            var matchingTypes = assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Select(x => new
                {
                    ViewType = x,
                    BaseViewType = ResolveBaseViewType(x)
                }).Where(x => x.BaseViewType != null).ToArray();
            if (!matchingTypes.Any())
            {
                return 0; 
            }

            logger.Debug($"Detected Blazor views in assembly:\n\t{matchingTypes.DumpToTable()}");
            foreach (var typeInfo in matchingTypes)
            {
                var blazorViewAttribute = typeInfo.ViewType.GetCustomAttribute<BlazorViewAttribute>();
                if (blazorViewAttribute != null && blazorViewAttribute.IsForManualRegistrationOnly)
                {
                    logger.Debug($"Skipping Blazor view {typeInfo} as it is marked for manual registration only");
                    continue;
                }

                logger.Debug($"Blazor view {typeInfo} view type inferred automatically {typeInfo.ViewType}");

                Type dataContextType;
                if (blazorViewAttribute != null && blazorViewAttribute.DataContextType != null)
                {
                    logger.Debug($"Blazor data context type has view type override set to {blazorViewAttribute.DataContextType}");
                    dataContextType = blazorViewAttribute.DataContextType;
                }
                else
                {
                    dataContextType = ResolveContentType(typeInfo.BaseViewType);
                    logger.Debug($"Blazor data context type has been inferred automatically from {typeInfo.BaseViewType} to {dataContextType}");
                }

                RegisterViewType(viewType: typeInfo.ViewType, dataContextType: dataContextType, key: blazorViewAttribute?.ViewTypeKey);
                logger.Debug($"Successfully registered Blazor view {typeInfo}");
                viewsRegistered++;
            }
        }
        catch (Exception e)
        {
            logger.Warn($"Failed to load Blazor views from assembly {new { assembly, assembly.Location }}", e);
        }
        return viewsRegistered;
    }

    private static Type ResolveContentType(Type type)
    {
        if (type == typeof(BlazorReactiveComponent))
        {
            return typeof(object);
        }

        var genericTypeDef = type.IsGenericType ? type.GetGenericTypeDefinition() : default;
        if (genericTypeDef != typeof(BlazorReactiveComponent<>))
        {
            throw new ArgumentException($"Expected base type to be {typeof(BlazorReactiveComponent<>)}, but was: {genericTypeDef}");
        }

        var genericTypeArguments = type.GetGenericArguments();
        if (genericTypeArguments.Length != 1)
        {
            throw new ArgumentException(
                $"Expected type {type} (generic: {genericTypeDef}) to have a single generic argument, but was: {genericTypeArguments.Select(x => x.ToString()).DumpToString()}");
        }

        return genericTypeArguments[0];
    }

    private static Type ResolveBaseViewTypeLegacy(Type type)
    {
        var genericTypeDef = type.IsGenericType ? type.GetGenericTypeDefinition() : default;
        if (genericTypeDef == typeof(BlazorReactiveComponent<>))
        {
            return type;
        }

        if (type.BaseType == null)
        {
            return null;
        }

        return ResolveBaseViewTypeLegacy(type.BaseType);
    }

    private static Type ResolveBaseViewType(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type == typeof(BlazorReactiveComponent))
            {
                return type;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BlazorReactiveComponent<>))
            {
                return type;
            }

            type = type.BaseType;
        }

        return null;
    }

    private readonly record struct ViewRegistration
    {
        public ViewKey Key { get; init; }
        public Type ViewType { get; init; }
        public DateTime RegistrationTimestamp { get; init; }
    }

    private readonly record struct ViewKey
    {
        public Type DataContextType { get; init; }
        public object Key { get; init; }
    }
}