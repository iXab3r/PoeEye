﻿using System.Text;

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
    
    public static string CutOffStackTrace(this Exception ex, Predicate<string> cutoffCondition)
    {
        var stackTrace = ex.StackTrace;
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            return stackTrace;
        }
        
        var lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        var result = new StringBuilder();

        foreach (var line in lines)
        {
            result.AppendLine(line);
            if (cutoffCondition(line))
            {
                return result.ToString();
            }
        }

        return result.ToString();
    }
}