using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Models
{
    public interface IPoeMessageParser
    {
        bool TryParse(PoeMessage message, out TradeModel result);
    }
}