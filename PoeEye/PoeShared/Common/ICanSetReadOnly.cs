namespace PoeShared.Common;

public interface ICanSetReadOnly : IHasReadOnly
{
    new bool IsReadOnly { get; set; }
}

public interface IHasReadOnly
{
    bool IsReadOnly { get; }
}