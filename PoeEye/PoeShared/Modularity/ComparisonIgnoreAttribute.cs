using System;

namespace PoeShared.Modularity
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ComparisonIgnoreAttribute : Attribute
    {
    }
}