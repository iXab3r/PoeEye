using System.Text;
using PropertyBinder;

namespace PoeShared.Bindings;

public sealed class PropertyPathWatcher : ExpressionWatcherBase
{
    private new static readonly Binder<PropertyPathWatcher> Binder;

    static PropertyPathWatcher()
    {
        Binder = ExpressionWatcherBase.Binder.Clone<PropertyPathWatcher>();
        Binder.Bind( x => x.SourceType.GetPropertyTypeOrDefault(x.PropertyPath))
            .To(x => x.PropertyType);
            
        Binder.Bind(x => !string.IsNullOrEmpty(x.PropertyPath) ? $@"{ICsharpExpressionParser.InputParameterName}.{x.PropertyPath}" : default ).To(x => x.SourceExpression);
        Binder.Bind(x => BuildCondition(x.PropertyPath)).To(x => x.ConditionExpression);
    }

    public string PropertyPath { get; set; }
        
    public PropertyPathWatcher()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    private static Type ResolveTypeByPathOrDefault(object source, string propertyPath)
    {
        if (source == null || string.IsNullOrEmpty(propertyPath))
        {
            return default;
        }

        var result = GetPropertyTypeOrDefault(source, propertyPath);
        return result;
    }
        
        
    private static Type GetPropertyTypeOrDefault(object model, string propertyPath)
    {
        if (model == null || string.IsNullOrEmpty(propertyPath))
        {
            return default;
        }

        var propertyParts = propertyPath.Split('.');
        var rootProperty = model.GetType().GetPropertyInfoOrDefault(propertyParts[0]);
        if (rootProperty == null)
        {
            return default;
        }

        if (propertyParts.Length <= 1)
        {
            return rootProperty.PropertyType;
        }
            
        var root = rootProperty.GetValue(model);
        if (root == null)
        {
            return default;
        }
        return GetPropertyTypeOrDefault(root, propertyParts.Skip(1).JoinStrings("."));
    }

    private static string BuildCondition(string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            return default;
        }
            
        var propertyParts = new[] { "" }.Concat(propertyPath.Split('.').SkipLast(1));
        var result = new StringBuilder();
        var combinedPropertyName = new StringBuilder();
        foreach (var propertyPart in propertyParts)
        {
            if (combinedPropertyName.Length > 0)
            {
                combinedPropertyName.Append(".");
            }

            if (result.Length > 0)
            {
                result.Append(" && ");
            }

            combinedPropertyName.Append(propertyPart);

            result.Append($"{ICsharpExpressionParser.InputParameterName}{(combinedPropertyName.Length > 0 ? "." : null)}{combinedPropertyName} != null");
        }
            
        return result.ToString();
    }
    
    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append($"PPW {PropertyPath}");
        builder.AppendParameter(nameof(Source), Source == default ? "not set" : Source.ToString());
        builder.AppendParameter(nameof(Value), Value);
        builder.AppendParameter(nameof(HasValue), HasValue);
    }
}