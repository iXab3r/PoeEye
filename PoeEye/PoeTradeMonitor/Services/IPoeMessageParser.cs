using PoeEye.TradeMonitor.Models;
using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Services
{
    internal interface IPoeMessageParser
    {
        bool TryParse(PoeMessage message, out TradeModel result);
    }
}