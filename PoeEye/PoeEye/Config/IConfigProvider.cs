namespace PoeEye.Config
{
    using System.ComponentModel;

    using JetBrains.Annotations;

    internal interface IConfigProvider<TConfig> : INotifyPropertyChanged
        where TConfig : class
    {
        TConfig ActualConfig { [NotNull] get; }

        void Reload();

        void Save([NotNull] TConfig config);
    }
}