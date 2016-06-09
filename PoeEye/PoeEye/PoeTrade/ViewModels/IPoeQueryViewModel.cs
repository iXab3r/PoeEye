using System;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeQueryViewModel : IPoeQueryInfo, IReactiveObject, IReactiveNotifyPropertyChanged<IReactiveObject>
    {
        [NotNull]
        IPoeQueryInfo GetQueryInfo();

        [NotNull]
        Func<IPoeQueryInfo> PoeQueryBuilder { get; }

        string Description { get; }

        new bool IsExpanded { get; set; }

        void SetQueryInfo([NotNull] IPoeQueryInfo source);
    }
}