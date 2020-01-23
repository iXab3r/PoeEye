using System.Windows.Markup;

namespace PoeShared.Wpf.Scaffolding
{
    public static class ObjectExtensions
    {
        public static TItem AddTo<TItem>(this TItem instance, IAddChild parent)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(parent, nameof(parent));

            parent.AddChild(instance);
            return instance;
        }
    }
}