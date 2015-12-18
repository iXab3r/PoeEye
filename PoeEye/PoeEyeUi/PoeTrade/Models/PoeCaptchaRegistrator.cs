namespace PoeEyeUi.PoeTrade.Models
{
    using System.Reactive.Subjects;

    internal sealed class PoeCaptchaRegistrator : IPoeCaptchaRegistrator
    {
        public ISubject<string> CaptchaRequests { get; } = new Subject<string>();
    }
}