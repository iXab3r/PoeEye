namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    public interface IPoeQuery
    {   
         IPoeQueryArgument[] Arguments { [NotNull] get; }
    }
}