namespace PoeEyeUi.PoeTrade.Models
{
    using System.Reactive.Subjects;

    using JetBrains.Annotations;

    internal interface IPoeCaptchaRegistrator
    {
        ISubject<string> CaptchaRequests { [NotNull] get; }
    }
}