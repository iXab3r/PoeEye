using System.Reactive.Subjects;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class PoeCaptchaRegistrator : IPoeCaptchaRegistrator
    {
        public ISubject<string> CaptchaRequests { get; } = new Subject<string>();
    }
}