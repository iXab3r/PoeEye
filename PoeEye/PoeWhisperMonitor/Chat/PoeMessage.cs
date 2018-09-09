using System;

namespace PoeWhisperMonitor.Chat
{
    public struct PoeMessage
    {
        public PoeMessageType MessageType { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"[{MessageType} {Name}]  {Message}";
        }
    }
}