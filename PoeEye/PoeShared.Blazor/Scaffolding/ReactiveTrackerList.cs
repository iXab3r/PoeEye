using System;
using System.Collections.Generic;

namespace PoeShared.Blazor.Scaffolding;

public sealed class ReactiveTrackerList : List<IObservable<string>>
{
    public ReactiveTrackerList()
    {
    }
}