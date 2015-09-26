namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.Common;

    internal interface IPoeTradeViewModel
    {
        PoeTradeState TradeState { get; set; }

        string Name { get; }

        string UserIgn { get; }

        string Price { get; }

        IPoeItem Trade { get; }
    }
}