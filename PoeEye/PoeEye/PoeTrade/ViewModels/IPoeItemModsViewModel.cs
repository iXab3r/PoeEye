using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeEye.PoeTrade.ViewModels 
{
    internal interface IPoeItemModsViewModel 
    {
        IPoeItem Item { [CanBeNull] get; [CanBeNull] set; }
        
        string Html { [CanBeNull] get; }
        
        string RawText { [CanBeNull] get; }
    }
}