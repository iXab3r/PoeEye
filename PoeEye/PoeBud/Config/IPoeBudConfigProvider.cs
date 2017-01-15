namespace PoeBud.Config
{
    using System;
    using System.Reactive;

    using JetBrains.Annotations;

    internal interface IPoeBudConfigProvider<TConfig> where TConfig : IPoeBudConfig
    {
        [NotNull]
        TConfig Load();

        void Save([NotNull] TConfig config);

        IObservable<Unit> ConfigUpdated { [NotNull] get; }
    }
}