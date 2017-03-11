using System;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Models
{
    internal struct TradeModel 
    {
        public PoePrice Price { get; set; }

        public string CharacterName { get; set; }

        public string League { get; set; }

        public string PositionName { get; set; }

        public DateTime Timestamp { get; set; }

        public string TabName { get; set; }

        public ItemPosition ItemPosition { get; set; }

        public string Offer { get; set; }

        public TradeType TradeType { get; set; }
    }
}