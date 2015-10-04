namespace PoeEyeUi.PoeTrade.Models
{
    using System.Collections.Generic;
    using System.Text;

    internal sealed class FuzzySearchService
    {
        private const int MinLength = 2;
        private readonly Dictionary<string, List<string>> index;

        public FuzzySearchService(IEnumerable<string> valuesToIndex)
        {
            index = Init(valuesToIndex);
        }

        private IEnumerable<string> ParseValue(string value)
        {
            return value.ToLower().Split(' ');
        }

        private Dictionary<string, List<string>> Init(IEnumerable<string> valuesToIndex)
        {
            var indexToBuild = new Dictionary<string, List<string>>();

            foreach (var item in valuesToIndex)
            {
                var parsedInputs = ParseValue(item);
                foreach (var value in parsedInputs)
                {
                    var hash = Soundex(value);
                    List<string> currentlyIndexedItems;
                    if (indexToBuild.TryGetValue(hash, out currentlyIndexedItems))
                    {
                        currentlyIndexedItems.Add(item);
                    }
                    else
                    {
                        indexToBuild.Add(hash, new List<string> {item});
                    }
                }
            }
            return indexToBuild;
        }

        public IEnumerable<SearchResult> Search(string inputToSearch)
        {
            var results = new HashSet<SearchResult>();
            foreach (var input in ParseValue(inputToSearch))
            {
                List<string> valuesFromIndex;
                if (index.TryGetValue(Soundex(input), out valuesFromIndex))
                {
                    foreach (var resultFromIndex in valuesFromIndex)
                    {
                        results.Add(new SearchResult(inputToSearch, resultFromIndex,
                            Score(inputToSearch, resultFromIndex)));
                    }
                }
            }

            return results;
        }

        private double Score(string in1, string in2)
        {
            return DicesCoeffienct(in1.ToLower(), in2.ToLower())*100;
        }

        private double DicesCoeffienct(string in1, string in2)
        {
            var nx = new HashSet<string>();
            var ny = new HashSet<string>();

            for (var i = 0; i < in1.Length - 1; i++)
            {
                var x1 = in1[i];
                var x2 = in1[i + 1];
                string temp = x1.ToString() + x2.ToString();
                nx.Add(temp);
            }
            for (var j = 0; j < in2.Length - 1; j++)
            {
                var y1 = in2[j];
                var y2 = in2[j + 1];
                string temp = y1.ToString() + y2.ToString();
                ny.Add(temp);
            }

            var intersection = new HashSet<string>(nx);
            intersection.IntersectWith(ny);

            double dbOne = intersection.Count;
            return (2*dbOne)/(nx.Count + ny.Count);
        }


        public class SearchResult
        {
            public SearchResult(string searched, string result, double score)
            {
                Searched = searched;
                Result = result;
                Score = score;
            }

            public string Searched { get; }

            public string Result { get; }

            public double Score { get; }

            public override string ToString()
            {
                return string.Format("Searched: {0}, SearchResult: {1}, Score: {2}", Searched, Result, Score);
            }

            protected bool Equals(SearchResult other)
            {
                return string.Equals(Searched, other.Searched) && string.Equals(Result, other.Result) &&
                       Score.Equals(other.Score);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != GetType())
                {
                    return false;
                }
                return Equals((SearchResult) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Searched != null ? Searched.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Result != null ? Result.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ Score.GetHashCode();
                    return hashCode;
                }
            }
        }

        #region Soundex

        private string EncodeChar(char c)
        {
            switch (char.ToLower(c))
            {
                case 'b':
                case 'f':
                case 'p':
                case 'v':
                    return "1";
                case 'c':
                case 'g':
                case 'j':
                case 'k':
                case 'q':
                case 's':
                case 'x':
                case 'z':
                    return "2";
                case 'd':
                case 't':
                    return "3";
                case 'l':
                    return "4";
                case 'm':
                case 'n':
                    return "5";
                case 'r':
                    return "6";
                default:
                    return string.Empty;
            }
        }

        private string Soundex(string data)
        {
            var result = new StringBuilder();

            if (!string.IsNullOrEmpty(data))
            {
                string previousCode, currentCode;
                result.Append(char.ToUpper(data[0]));
                previousCode = string.Empty;

                for (var i = 1; i < data.Length; i++)
                {
                    currentCode = EncodeChar(data[i]);

                    if (currentCode != previousCode)
                    {
                        result.Append(currentCode);
                    }

                    if (result.Length == MinLength)
                    {
                        break;
                    }

                    if (!currentCode.Equals(string.Empty))
                    {
                        previousCode = currentCode;
                    }
                }
            }
            if (result.Length < MinLength)
            {
                result.Append(new string('0', MinLength - result.Length));
            }

            return result.ToString();
        }

        #endregion
    }
}