using System;

namespace PoeShared.Modularity
{
    /// <summary>
    ///   Module will not be loaded by default
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IgnoreOnDiscoveryModule : Attribute
    {
    }
}