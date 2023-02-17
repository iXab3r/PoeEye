using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    
    public static void CollectionSequenceShouldBe(this IList instance, IList expected, [CallerMemberName] string shouldlyMethod = null)
    {
        if (expected == null)
        {
            instance.ShouldBeNull();
            return;
        }
        instance.Count.ShouldBe(expected.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            instance[i].ShouldBe(expected[i]);
        }
    }
}