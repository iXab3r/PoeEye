using System;
using System.Collections.Generic;
using System.Linq;
using Dragablz;
using PoeShared;
using PoeShared.Scaffolding;

namespace PoeEye.Utilities
{
    internal sealed class TabablzPositionMonitor<T> : VerticalPositionMonitor
    {
        public TabablzPositionMonitor()
        {
            Items = Enumerable.Empty<T>();
            this.OrderChanged += OnOrderChanged;
        }

        private void OnOrderChanged(object sender, OrderChangedEventArgs orderChangedEventArgs)
        {
            if (sender == null || orderChangedEventArgs == null)
            {
                return;
            }

            Log.Instance.Debug($"[PositionMonitor] Items order has changed, old: {orderChangedEventArgs.PreviousOrder.DumpToText()}, new: {orderChangedEventArgs.NewOrder.DumpToText()}");
            Items = orderChangedEventArgs.NewOrder
                .EmptyIfNull()
                .OfType<T>()
                .ToArray();
        }

        public IEnumerable<T> Items { get; private set; }
    }
}