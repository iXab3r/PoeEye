using Guards;
using JetBrains.Annotations;

namespace PoeShared.PoeTrade.Query
{
    public abstract class PoeQueryArgumentBase : IPoeQueryArgument
    {
        protected PoeQueryArgumentBase([NotNull] string name)
        {
            Guard.ArgumentNotNull(name, nameof(name));

            Name = name;
        }

        public string Name { get; }
    }
}
