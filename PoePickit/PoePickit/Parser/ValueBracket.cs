namespace PoePricer.Parser
{
    internal struct ValueBracket
    {
        public int ItemLevel { get; set; }

        public int FirstAffixValueLo { get; set; }

        public int FirstAffixValueHi { get; set; }

        public int SecondAffixValueLo { get; set; }

        public int SecondAffixValueHi { get; set; }
    }
}