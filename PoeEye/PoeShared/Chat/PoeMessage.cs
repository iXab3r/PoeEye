namespace PoeShared.Chat
{
    using System;

    public struct PoeMessage
    {
        public PoeMessageType MessageType { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"[{MessageType}] {Message}";
        }
    }
}