using System.Linq;
using System.Text;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Bindings
{
    public sealed class PropertyPathWatcher : ExpressionWatcherBase
    {
        private new static readonly Binder<PropertyPathWatcher> Binder;

        static PropertyPathWatcher()
        {
            Binder = ExpressionWatcherBase.Binder.Clone<PropertyPathWatcher>();
            Binder.Bind( x => x.SourceType.GetPropertyTypeOrDefault(x.PropertyPath))
                .To(x => x.PropertyType);
            
            Binder.Bind(x => !string.IsNullOrEmpty(x.PropertyPath) ? $@"x.{x.PropertyPath}" : default ).To(x => x.SourceExpression);
            Binder.Bind(x => BuildCondition(x.PropertyPath)).To(x => x.ConditionExpression);
        }

        public string PropertyPath { get; set; }

        public PropertyPathWatcher()
        {
            Binder.Attach(this).AddTo(Anchors);
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

                result.Append($"x{(combinedPropertyName.Length > 0 ? "." : null)}{combinedPropertyName} != null");
            }
            
            return result.ToString();
        }

        public override string ToString()
        {
            return $"PropertyPathWatcher for {PropertyPath}, source: {Source}, sourceType: {SourceType}";
        }
    }
}