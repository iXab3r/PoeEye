using System;
using Newtonsoft.Json;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    public interface IPoeEyeConfig
    {
    }

    public interface IPoeEyeConfigVersioned : IPoeEyeConfig
    {
        /// <summary>
        ///   Config contract version
        /// </summary>
        int Version { get; }
    }
}