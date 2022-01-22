using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace PoeShared.Tests.Helpers;

public sealed class TestExtensions
{
    public static EventArgs Raises(object raiser, string eventName, Action function)
    {
        return Raises<EventArgs>(raiser, eventName, function);
    }

    public static T Raises<T>(object raiser, string eventName, Action function) where T:EventArgs
    {
        var listener = new EventListener<T>(raiser, eventName);
        function.Invoke();
        listener.SavedArgs.Count.ShouldBe(1);
        return listener.SavedArgs[0];
    }

    public static IList<T> RaisesMany<T>(object raiser, string eventName, Action function) where T : EventArgs
    {
        var listener = new EventListener<T>(raiser, eventName);
        function.Invoke();
        return listener.SavedArgs;
    }

}