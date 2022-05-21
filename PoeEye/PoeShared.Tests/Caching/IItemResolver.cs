namespace PoeShared.Tests.Caching;

public interface IItemResolver<in TKey, TValue>
{
    TValue Resolve(TKey key, TValue existing);
}