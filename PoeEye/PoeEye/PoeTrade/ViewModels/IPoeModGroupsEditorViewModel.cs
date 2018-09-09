using PoeShared.PoeTrade.Query;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeModGroupsEditorViewModel
    {
        IReactiveList<IPoeModsEditorViewModel> Groups { get; }

        IPoeModsEditorViewModel AddGroup();

        IPoeQueryModsGroup[] ToGroups();
    }
}