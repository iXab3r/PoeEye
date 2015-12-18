using System.Reactive.Subjects;

namespace PoeEyeUi.PoeTrade.Models
{
    using JetBrains.Annotations;

    internal interface IPoeCaptchaRegistrator
    {
        ISubject<string> CaptchaRequests { [NotNull] get; }
    }
}