using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reflection;
using PoeShared.Scaffolding;
using PoeShared.Services;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using PoeShared.Logging;

namespace PoeShared.Blazor.Services;

public class BlazorViewRepository : DisposableReactiveObjectWithLogger, IBlazorViewRepository, IBlazorViewRegistrator
{
    private readonly IClock clock;
    private readonly ConcurrentQueue<Assembly> unprocessedAssemblies = new();
    private readonly ConcurrentDictionary<ViewKey, ViewRegistration> viewsByKey = new();

    public BlazorViewRepository(
        IClock clock,
        IAssemblyTracker assemblyTracker)
    {
        this.clock = clock;
        this.WhenAnyValue(x => x.AutomaticallyProcessAssemblies)
            .Select(x => x ? assemblyTracker.WhenLoaded : Observable.Empty<Assembly>())
            .Switch()
            .Subscribe(x =>
            {
                Log.Debug($"Adding assembly {x} to processing queue, size: {unprocessedAssemblies.Count}");
                unprocessedAssemblies.Enqueue(x);
                Log.Debug($"Added assembly {x} to processing queue");
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }
    
    public bool AutomaticallyProcessAssemblies { get; set; } = true;
    
    public void RegisterViewType(Type viewType, object key = default)
    {
        EnsureQueueIsProcessed();
        var baseViewType = ResolveBaseViewType(viewType);
        var contentType = ResolveContentType(baseViewType);
        RegisterViewType(viewType, contentType, key);
    }
    
    private void RegisterViewType(Type viewType, Type viewContentType, object key)
    {
        var viewKey = new ViewKey()
        {
            ContentType = viewContentType, 
            Key = key
        };
        var log = Log.WithSuffix(viewKey.ToString());
        log.Debug(() => $"Registering view type");

        var registration = new ViewRegistration()
        {
            RegistrationTimestamp = clock.Now,
            ViewType = viewType
        };
        viewsByKey.AddOrUpdate(viewKey, () =>
        {
            log.Debug(() => $"Registered new view type: {registration}");
            return registration;
        }, (_, existingRegistration) =>
        {
            log.Debug(() => $"Overriding registration with a new view type: {registration}, existing: {existingRegistration}");
            return registration;
        });
    }
    
    public Type ResolveViewType(Type contentType, object key = default)
    {
        var viewKey = new ViewKey() { ContentType = contentType, Key = key };
        var log = Log.WithSuffix(viewKey.ToString());
        log.Debug(() => $"Resolving view type");
        EnsureQueueIsProcessed();

        if (!viewsByKey.TryGetValue(viewKey, out var registration))
        {
            log.Warn(() => $"Failed to resolve registered view, known views:\n\t{viewsByKey.Keys.DumpToTable()}");
            return null;
        }    
        
        log.Debug(() => $"Resolved registered view: {registration}");
        return registration.ViewType;
    }
    
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void EnsureQueueIsProcessed()
    {
        while (unprocessedAssemblies.TryDequeue(out var assembly))
        {
            Log.Info($"Detected unprocessed assemblies({unprocessedAssemblies.Count}), processing {assembly}");
            LoadViewsFromAssembly(assembly);
        }
    }
    
    private void LoadViewsFromAssembly(Assembly assembly)
    {
        var logger = Log.WithSuffix(assembly.ToString());
        logger.Debug(() => "Loading Blazor views from assembly");

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
                return;
            }
            logger.Debug(() => $"Detected Blazor views in assembly:\n\t{matchingTypes.DumpToTable()}");
            foreach (var typeInfo in matchingTypes)
            {
                var blazorViewAttribute = typeInfo.ViewType.GetCustomAttribute<BlazorViewAttribute>();
                if (blazorViewAttribute != null && blazorViewAttribute.IsForManualRegistrationOnly)
                {
                    logger.Debug(() => $"Skipping Blazor view {typeInfo} as it is marked for manual registration only");
                    continue;
                }

                var contentType = ResolveContentType(typeInfo.BaseViewType);
                RegisterViewType(viewType: typeInfo.ViewType, viewContentType: contentType, key: blazorViewAttribute?.ViewKey);
                logger.Debug(() => $"Successfully registered Blazor view {typeInfo}");
            }
        }
        catch (Exception e)
        {
            logger.Warn($"Failed to load Blazor views from assembly {new { assembly, assembly.Location }}", e);
        }
    }
    
    private static Type ResolveContentType(Type type)
    {
        var genericTypeDef = type.IsGenericType ? type.GetGenericTypeDefinition() : default;
        if (genericTypeDef != typeof(BlazorReactiveComponent<>))
        {
            throw new ArgumentException($"Expected base type to be {typeof(BlazorReactiveComponent<>)}, but was: {genericTypeDef}");
        }

        var genericTypeArguments = type.GetGenericArguments();
        if (genericTypeArguments.Length != 1)
        {
            throw new ArgumentException($"Expected type {type} (generic: {genericTypeDef}) to have a single generic argument, but was: {genericTypeArguments.Select(x => x.ToString()).DumpToString()}");
        }

        return genericTypeArguments[0];
    }
    
    private static Type ResolveBaseViewType(Type type)
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

        return ResolveBaseViewType(type.BaseType);
    }

    private readonly record struct ViewRegistration
    {
        public Type ViewType { get; init; }
        public DateTime RegistrationTimestamp { get; init; }
    }

    private readonly record struct ViewKey
    {
        public Type ContentType { get; init; }
        public object Key { get; init; }
    }
}