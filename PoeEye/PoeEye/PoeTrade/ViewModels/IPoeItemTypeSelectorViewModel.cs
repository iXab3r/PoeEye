using System.Collections.ObjectModel;
using PoeShared.Common;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeItemTypeSelectorViewModel
    {
        string SelectedValue { get; set; }
        ReadOnlyObservableCollection<string> KnownItemTypes { get; }
        IPoeItemType ToItemType();
    }
}