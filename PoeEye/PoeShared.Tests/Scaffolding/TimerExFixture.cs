using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class TimerExFixture
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void ShouldCreate()
    {
        // Given
        // When 
        Action action = () => CreateInstance();

        // Then
        action.ShouldNotThrow();
    }

    [Test]
    [TestCaseSource(nameof(ShouldProcessOneAfterAnotherCases))]
    public void ShouldProcessOneAfterAnother(Func<IObservable<long>> replacementFunc)
    {
        //Given
        var queue = new Queue<(int, string)>();
        var timer = replacementFunc == null ? CreateInstance() : replacementFunc();

        var idx = 0;
        //When
        timer.TakeWhile(_ => idx < 1000).Do(_ =>
        {
            queue.Enqueue((idx, $"#{idx} => #{Interlocked.Increment(ref idx)}"));
            Thread.Sleep(1);
        }).Wait();

        //Then
        queue.Count.ShouldBe(1000);
        queue.Select(x => x.Item1).ShouldBeInOrder();
    }

    public static IEnumerable<TestCaseData> ShouldProcessOneAfterAnotherCases()
    {
        yield return new TestCaseData(new Func<IObservable<long>>(() => Observable.Timer(TimeSpan.Zero, TimeSpan.Zero)));
        yield return new TestCaseData(default);
    }

    private TimerEx CreateInstance()
    {
        return fixture.Build<TimerEx>().OmitAutoProperties().Create();
    }
}