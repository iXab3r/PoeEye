namespace PoeShared.Native
{
    public sealed class FakeStringMatcher : IStringMatcher
    {
        private readonly bool value;

        public FakeStringMatcher(bool value)
        {
            this.value = value;
        }

        public bool IsMatch(string value)
        {
            return this.value;
        }
    }
}