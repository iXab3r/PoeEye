using System.ComponentModel;

namespace PoeShared.Common
{
    public enum PoeBuyoutMode
    {
        [Description("Show all items")] 
        Unknown,
        [Description("Show items with buyout")]
        BuyoutOnly,
        [Description("Show items without buyout")]
        OfferOnly
    }
}