namespace FuzzySearch
{
    public struct SearchResult
    {
        public SearchResult(string result, double score) : this(result, score, null)
        {
        }

        public SearchResult(string result, double score, object match)
        {
            Result = result;
            Score = score;
            Match = match;
        }

        public string Result { get; }

        public object Match { get; }

        public double Score { get; }

        public override string ToString()
        {
            return $"SearchResult: {Result}, Score: {Score}";
        }
    }
}