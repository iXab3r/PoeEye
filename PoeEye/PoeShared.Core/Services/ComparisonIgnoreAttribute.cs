using System;

namespace PoeShared.Services
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ComparisonIgnoreAttribute : Attribute
    {
    }
}