using System.ComponentModel;

namespace PoeShared.Common
{
    public enum PoeBuyoutMode
    {
        [Description("any")]
        Unknown,

        [Description("w/ buyout")]
        BuyoutOnly,

        [Description("w/o buyout")]
        OfferOnly
    }
}