namespace PoePricer.Parser
{
    internal struct FilterArg
    {
        public string Name { get; set; }

        public ArgOperators Operator { get; set; }

        public double Value { get; set; }

        public ArgTypes Type { get; set; }
    }
}