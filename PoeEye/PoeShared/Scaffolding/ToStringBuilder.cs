﻿using System.Text;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public sealed class ToStringBuilder
{
    private static readonly IFluentLog Log = typeof(ToStringBuilder).PrepareLogger();

    private readonly StringBuilder primaryPartBuilder = new();
    private readonly StringBuilder paramsPartBuilder = new();
    private readonly string ownerName;
    private string paramsSeparator = ", ";

    public ToStringBuilder([NotNull] string ownerName)
    {
        this.ownerName = ownerName;
    }

    public ToStringBuilder([NotNull] object owner) : this(owner.GetType().Name)
    {
    }

    public ToStringBuilder WithParamsSeparator(string paramsSeparator)
    {
        var result = new ToStringBuilder(ownerName)
        {
            paramsSeparator = paramsSeparator
        };
        return result;
    }

    public ToStringBuilder Append(string value)
    {
        primaryPartBuilder.Append(value);
        return this;
    }

    public ToStringBuilder AppendParameterIfNotDefault<T>(string parameterName, T value)
    {
        try
        {
            if (Equals(value, default(T)) || (value is string stringValue && string.IsNullOrEmpty(stringValue)))
            {
                return this;
            }

            AppendParameter(parameterName, value);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to format nullable parameter {parameterName}", e);
            paramsPartBuilder.Append($"{parameterName} ERROR: {e}");
        }

        return this;
    }

    public ToStringBuilder AppendParameter<T>(string parameterName, T value)
    {
        try
        {
            if (paramsPartBuilder.Length > 0)
            {
                paramsPartBuilder.Append(paramsSeparator);
            }

            paramsPartBuilder.Append($"{parameterName}: {value}");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to format parameter {parameterName}", e);
            paramsPartBuilder.Append($"{parameterName} ERROR: {e}");
        }

        return this;
    }

    public string ParametersToString()
    {
        return paramsPartBuilder.ToString();
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
                result.Append(ownerName);
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