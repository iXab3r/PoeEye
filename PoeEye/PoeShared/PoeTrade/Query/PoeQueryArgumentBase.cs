namespace PoeShared.PoeTrade.Query
{
    using Guards;

    using JetBrains.Annotations;

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
