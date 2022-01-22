using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace PoeShared.Tests.Helpers;

public static class EnumerableExtensions
{
    public static void CollectionShouldBe<T>(this IEnumerable<T> instance, params T[] args)
    {
        instance.OrderBy(x => x).ShouldBe(args.OrderBy(x => x));
    }
        
    public static void CollectionSequenceShouldBe<T>(this IEnumerable<T> instance, params T[] args)
    {
        instance.ShouldBe(args);
    }
}