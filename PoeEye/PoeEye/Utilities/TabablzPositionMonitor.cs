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

            Log.Instance.Trace($"[PositionMonitor] Items order has changed, \nOld:\n\t{orderChangedEventArgs.PreviousOrder.EmptyIfNull().Select(x => x?.ToString() ?? "(null)").DumpToTable()}, \nNew:\n\t{orderChangedEventArgs.NewOrder.EmptyIfNull().Select(x => x?.ToString() ?? "(null)").DumpToTable()}");
            Items = orderChangedEventArgs.NewOrder
                .EmptyIfNull()
                .OfType<T>()
                .ToArray();
        }

        public IEnumerable<T> Items { get; private set; }
    }
}