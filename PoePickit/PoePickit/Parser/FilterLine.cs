using System.Collections.Generic;

namespace PoePricer.Parser
{
    internal struct FilterLine
    {
        public List<FilterArg> Args;

        public FilterTiers Tier { get; set; }

        public ToolTipTypes ToolTipType { get; set; }

        public string RawLine { get; set; }
    }
}