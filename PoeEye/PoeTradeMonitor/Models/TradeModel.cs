using System;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Models
{
    public struct TradeModel 
    {
        public PoePrice Price { get; set; }

        public string CharacterName { get; set; }

        public string ItemName { get; set; }

        public DateTime Timestamp { get; set; }
    }
}