namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    internal interface IPoeTradeCaptchaViewModel : IDisposable
    {
        bool IsOpen { get; }

        string CaptchaUri { get; }
    }
}