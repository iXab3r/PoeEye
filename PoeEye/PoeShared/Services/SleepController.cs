using JetBrains.Annotations;

namespace PoeShared.Services;

internal sealed class SleepController : ISleepController
{
    private static readonly IFluentLog Log = typeof(SleepController).PrepareLogger();

    private static readonly Lazy<SleepController> InstanceSupplier = new(() => new SleepController());
    
    private SleepController()
    {
        Provider = new DefaultSleepProvider();
    }

    public static SleepController Instance => InstanceSupplier.Value;

    public ISleepProvider Provider { get; private set; }
    
    public void SetProvider([NotNull] ISleepProvider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }
        
        Log.Info($"Updating sleep provider: {Provider} => {provider}");
        Provider = provider;
    }
}