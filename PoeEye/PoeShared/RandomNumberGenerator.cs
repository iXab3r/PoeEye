using System.Threading;

namespace PoeShared
{
    internal sealed class RandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly ThreadSafeRandom rng = new();

        public int Next(int min, int max)
        {
            return rng.Next(min, max);
        }

        public int Next()
        {
            return rng.Next();
        }

        public int Next(int max)
        {
            return rng.Next(max);
        }

        public double NextDouble()
        {
            return rng.NextDouble();
        }
    }
}