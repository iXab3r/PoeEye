using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IHasError
{
    string Error { [CanBeNull] get; }
}