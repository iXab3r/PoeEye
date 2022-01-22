using System.Windows.Markup;

namespace PoeShared.Scaffolding;

public static class WpfObjectExtensions
{
    public static TItem AddTo<TItem>(this TItem instance, IAddChild parent)
    {
        Guard.ArgumentNotNull(instance, nameof(instance));
        Guard.ArgumentNotNull(parent, nameof(parent));

        parent.AddChild(instance);
        return instance;
    }
}