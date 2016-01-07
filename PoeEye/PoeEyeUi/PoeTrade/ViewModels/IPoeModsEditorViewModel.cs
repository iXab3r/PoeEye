using ReactiveUI;

namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.PoeTrade.Query;

    internal interface IPoeModsEditorViewModel
    {
        IReactiveList<IPoeModViewModel> Mods { get; }

        PoeQueryModsGroupType GroupType { get; set; }

        float? MinGroupValue { get; set; }

        float? MaxGroupValue { get; set; }

        IPoeModViewModel AddMod();

        IPoeQueryModsGroup ToGroup();
    }
}