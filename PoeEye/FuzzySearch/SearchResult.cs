namespace FuzzySearch
{
    public struct SearchResult
    {
        public SearchResult(string result, double score)
        {
            Result = result;
            Score = score;
        }

        public string Result { get; }

        public double Score { get; }

        public override string ToString()
        {
            return $"SearchResult: {Result}, Score: {Score}";
        }
    }
}