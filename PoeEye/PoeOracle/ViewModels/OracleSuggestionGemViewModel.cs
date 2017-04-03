using System;
using System.Diagnostics;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeOracle.Models;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI.ViewModels;
using Prism.Commands;

namespace PoeOracle.ViewModels
{
    internal sealed class OracleSuggestionGemViewModel : OracleSuggestionViewModelBase
    {
        private readonly SkillGemModel gemModel;
        private readonly IFactory<IImageViewModel, Uri> imageFactory;
        [NotNull] private readonly IExternalUriOpener uriOpener;
        private readonly DelegateCommand gotoGamepediaCommand;

        public OracleSuggestionGemViewModel(
            [NotNull] SkillGemModel gemModel,
            [NotNull] IFactory<IImageViewModel, Uri> imageFactory,
            [NotNull] IExternalUriOpener uriOpener)
        {
            Guard.ArgumentNotNull(gemModel, nameof(gemModel));
            Guard.ArgumentNotNull(imageFactory, nameof(imageFactory));
            Guard.ArgumentNotNull(uriOpener, nameof(uriOpener));

            this.gemModel = gemModel;
            this.imageFactory = imageFactory;
            this.uriOpener = uriOpener;

            if (gemModel.IconUri != null)
            {
                Icon = imageFactory.Create(gemModel.IconUri);
                Icon.AddTo(Anchors);
            }
            gotoGamepediaCommand = new DelegateCommand(GotoGamepediaCommandExecuted);
        }

        public string Name => gemModel.Name;

        public string CanBeBoughtBy => gemModel.CanBeBoughtBy.ToString();

        public string SoldBy => gemModel.SoldBy;

        public string RewardFor => gemModel.RewardFor.ToString();

        public string QualityBonus => gemModel.QualityBonus;

        public int RequiredLevel => gemModel.RequiredLevel;

        public IImageViewModel Icon { get; }

        public ICommand GotoGamepediaCommand => gotoGamepediaCommand;

        private void GotoGamepediaCommandExecuted()
        {
            var wikiUri = $"http://pathofexile.gamepedia.com/index.php?search={Name}";
            uriOpener.Request(wikiUri);
        }
    }
}
