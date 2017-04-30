using System;
using System.Collections.Generic;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using PoeShared.Common;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Models
{
    internal struct TradeModel 
    {
        private static readonly Lazy<IEqualityComparer<TradeModel>> ComparerSupplier = new Lazy<IEqualityComparer<TradeModel>>(() => new TradeModelComparer());

        public static IEqualityComparer<TradeModel> Comparer = ComparerSupplier.Value;

        public PoePrice Price { get; set; }

        public string CharacterName { get; set; }

        public string League { get; set; }

        public string PositionName { get; set; }

        public DateTime Timestamp { get; set; }

        public string TabName { get; set; }

        public ItemPosition ItemPosition { get; set; }

        public string Offer { get; set; }

        public TradeType TradeType { get; set; }

        private sealed class TradeModelComparer : IEqualityComparer<TradeModel>
        {
            public bool Equals(TradeModel x, TradeModel y)
            {
                var logic = new CompareLogic
                {
                    Config = new ComparisonConfig
                    {
                        MembersToInclude = new List<string>
                        {
                            nameof(CharacterName),
                            nameof(League),
                            nameof(ItemPosition),
                            nameof(PositionName),
                            nameof(TabName),
                            nameof(TradeType),
                            nameof(Price),
                        }
                    }
                };

                var result = logic.Compare(x, y);
                return result.AreEqual;
            }

            public int GetHashCode(TradeModel obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}