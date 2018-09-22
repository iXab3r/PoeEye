using JetBrains.Annotations;

namespace PoeShared.PoeTrade.Query
{
    internal interface IPoeStaticDataProvider
    {
        IPoeStaticData StaticData { [CanBeNull] get; }

        bool IsBusy { get; }

        string Error { [CanBeNull] get; }
    }
}