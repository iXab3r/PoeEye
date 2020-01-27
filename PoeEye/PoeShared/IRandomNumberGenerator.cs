namespace PoeShared
{
    public interface IRandomNumberGenerator
    {
        int Next(int min, int max);

        int Next();

        int Next(int max);
    }
}