namespace PoeShared
{
    using System;

    internal sealed class RandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly Random rng = new Random();

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
    }
}