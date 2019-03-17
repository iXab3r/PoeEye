using System.Threading.Tasks;
using Common.Logging;
using PoeEye.TradeSummaryOverlay.Modularity;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.TradeSummaryOverlay.ViewModels
{
    internal sealed class PoeTradeSummarySettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeTradeSummaryConfig>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeTradeSummarySettingsViewModel));

        private readonly PoeTradeSummaryConfig resultingConfig = new PoeTradeSummaryConfig();

        private bool isEnabled;

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public string ModuleName => "Realtime Trade Log";

        public async Task Load(PoeTradeSummaryConfig config)
        {
            config.CopyPropertiesTo(resultingConfig);

            IsEnabled = config.IsEnabled;
        }

        public PoeTradeSummaryConfig Save()
        {
            resultingConfig.IsEnabled = IsEnabled;

            var result = new PoeTradeSummaryConfig();
            resultingConfig.CopyPropertiesTo(result);

            return result;
        }
    }
}