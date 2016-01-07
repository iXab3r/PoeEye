using ReactiveUI;

namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.PoeTrade.Query;

    internal interface IPoeModGroupsEditorViewModel 
    {
        IReactiveList<IPoeModsEditorViewModel> Groups { get; }

        IPoeModsEditorViewModel AddGroup();

        IPoeQueryModsGroup[] ToGroups();
    }
}