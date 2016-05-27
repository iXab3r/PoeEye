namespace PoePricer.Parser
{
    public struct UniqueAffix
    {
        public string[] WordsInLine;
        public string RawLine;
        public double LoValue;
        public double HighValue;
        public double LowValueSecond;
        public double HighValueSecond;
        public bool IsImplicit;
        public bool IsDoubleAffix;
    }
}