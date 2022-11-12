using System.Text;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public sealed class ToStringBuilder
{
    private static readonly IFluentLog Log = typeof(ToStringBuilder).PrepareLogger();

    private readonly StringBuilder primaryPartBuilder = new();
    private readonly StringBuilder paramsPartBuilder = new();
    private readonly object owner;

    public ToStringBuilder([NotNull] object owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Append(string value)
    {
        primaryPartBuilder.Append(value);
    }

    public void AppendParameterIfNotDefault<T>(string parameterName, T value)
    {
        try
        {
            if (Equals(value, default(T)) || (value is string stringValue && string.IsNullOrEmpty(stringValue)))
            {
                return;
            }
            AppendParameter(parameterName, value);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to format nullable parameter {parameterName}", e);
            paramsPartBuilder.Append($"{parameterName} ERROR: {e}");
        }
    }
    
    public void AppendParameter<T>(string parameterName, T value)
    {
        try
        {
            if (paramsPartBuilder.Length > 0)
            {
                paramsPartBuilder.Append(", ");
            }

            paramsPartBuilder.Append($"{parameterName}: {value}");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to format parameter {parameterName}", e);
            paramsPartBuilder.Append($"{parameterName} ERROR: {e}");
        }
    }

    public override string ToString()
    {
        try
        {
            var result = new StringBuilder();
            if (primaryPartBuilder.Length > 0)
            {
                result.Append(primaryPartBuilder);
            }
            else
            {
                result.Append(owner.GetType().Name);
            }
            if (paramsPartBuilder.Length > 0)
            {
                result.Append($"{{ {paramsPartBuilder} }}");
            }

            return result.ToString();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to perform ToString()", e);
            return $"ToString() ERROR: {e}";
        }
    }
}