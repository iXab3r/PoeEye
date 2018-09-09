using System.Reactive.Subjects;
using JetBrains.Annotations;

namespace PoeEye.PoeTrade.Models
{
    internal interface IPoeCaptchaRegistrator
    {
        ISubject<string> CaptchaRequests { [NotNull] get; }
    }
}