﻿using System.Reactive.Concurrency;
using System.Threading;
using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface ISchedulerProvider
{
    [NotNull]
    IScheduler GetOrAdd([NotNull] string name);
    
    [NotNull]
    IScheduler Add([NotNull] string name, ThreadPriority threadPriority);
}