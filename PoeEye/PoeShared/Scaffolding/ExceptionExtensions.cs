using System.Text;

namespace PoeShared.Scaffolding;

public static class ExceptionExtensions
{
    public static bool IsCancellation(this Exception exception)
    {
        return exception switch
        {
            OperationCanceledException or AggregateException {InnerExceptions: [OperationCanceledException]} => true,
            _ => false
        };
    }
    
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

    public static bool TryCutOffStackTrace(this Exception ex, Predicate<string> cutoffCondition, out string formattedStackTrace)
    {
        var stackTrace = ex.StackTrace;
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            formattedStackTrace = default;
            return false;
        }
        
        var lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        var result = new StringBuilder();

        foreach (var line in lines)
        {
            result.AppendLine(line);
            if (cutoffCondition(line))
            {
                formattedStackTrace = result.ToString().Trim('\n','\r');
                return true;
            }
        }

        formattedStackTrace = default;
        return false;
    }
    
    public static string CutOffStackTrace(this Exception ex, Predicate<string> cutoffCondition)
    {
        return TryCutOffStackTrace(ex, cutoffCondition, out var stackTrace) ? stackTrace : ex.StackTrace;
    }
}