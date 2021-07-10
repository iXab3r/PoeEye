using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Components;
using ReactiveUI.Validation.Contexts;

namespace PoeShared.Wpf.Scaffolding
{
    public abstract class ValidatableReactiveObject<T> : DisposableReactiveObject, IValidatableViewModel, INotifyDataErrorInfo where T : DisposableReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> hasErrors;

        protected ValidatableReactiveObject()
        {
            ValidationContext = new ValidationContext();

            hasErrors = this
                .IsValid()
                .Select(valid => !valid)
                .ToProperty(this, x => x.HasErrors);

            ValidationContext
                .ValidationStatusChange
                .CombineLatest(this.WhenAnyProperty(), (_, change) => change.EventArgs.PropertyName)
                .Where(name => name != nameof(HasErrors))
                .Select(name => new DataErrorsChangedEventArgs(name))
                .Subscribe(args => ErrorsChanged?.Invoke(this, args));
        }
        
        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <inheritdoc />
        public bool HasErrors => hasErrors.Value;

        /// <inheritdoc />
        public ValidationContext ValidationContext { get; }

        /// <summary>
        /// Returns a collection of error messages, required by the INotifyDataErrorInfo interface.
        /// </summary>
        /// <param name="propertyName">Property to search error notifications for.</param>
        /// <returns>A list of error messages, usually strings.</returns>
        /// <inheritdoc />
        public virtual IEnumerable GetErrors(string propertyName)
        {
            var memberInfoName = GetType()
                .GetMember(propertyName)
                .FirstOrDefault()?
                .ToString();

            if (memberInfoName == null)
            {
                return Enumerable.Empty<string>();
            }

            var relatedPropertyValidations = ValidationContext
                .Validations
                .OfType<BasePropertyValidation<T>>()
                .Where(validation => validation.ContainsPropertyName(memberInfoName));

            return relatedPropertyValidations
                .Where(validation => !validation.IsValid)
                .SelectMany(validation => validation.Text)
                .ToList();
        }
    }
}