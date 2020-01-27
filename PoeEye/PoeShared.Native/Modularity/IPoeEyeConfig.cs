namespace PoeShared.Modularity
{
    public interface IPoeEyeConfig
    {
    }

    public interface IPoeEyeConfigVersioned : IPoeEyeConfig, IHasVersion
    {
    }

    public interface IHasVersion
    {
        /// <summary>
        ///     Config contract version
        /// </summary>
        int Version { get; set; }
    }
}