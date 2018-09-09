using System;

namespace PoeEye.PoeTrade.Models
{
    internal struct PoeItemPricePoint
    {
        public DateTime Timestamp { get; set; }

        public double Price { get; set; }
    }
}