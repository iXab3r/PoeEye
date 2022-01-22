using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeShared.Scaffolding;

public static class ExceptionExtensions
{
    public static Exception ToExceptionOrDefault(this Exception[] exceptions)
    {
        if (!exceptions.Any())
        {
            return default;
        }
            
        if (exceptions.Length == 1)
        {
            return exceptions[0];
        }

        return new AggregateException(exceptions);
    }
}