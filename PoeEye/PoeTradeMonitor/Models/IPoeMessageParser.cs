using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Models
{
    internal interface IPoeMessageParser
    {
        bool TryParse(PoeMessage message, out TradeModel result);
    }
}