namespace PoeEye.PoeTrade.ViewModels
{
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    internal interface IPoeModGroupsEditorViewModel
    {
        IReactiveList<IPoeModsEditorViewModel> Groups { get; }

        IPoeModsEditorViewModel AddGroup();

        IPoeQueryModsGroup[] ToGroups();
    }
}