using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Forms;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Scaffolding;

public static class ValidatorExtensions
{
    private static readonly IFluentLog Log = typeof(ValidatorExtensions).PrepareLogger();

    public static IRuleBuilderOptions<T, TProperty> MustSafeAsync<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Func<TProperty, CancellationToken, Task<bool>> predicate) {
        return ruleBuilder.MustAsync((x, val, context, cancel) =>
        {
            try
            {
                return predicate(val, cancel);
            }
            catch (Exception e)
            {
                Log.Warn($"Something went wrong", e);
                return Task.FromResult(false);
            }
        });
    }
        
    public static void RuleForError<T>(this AbstractValidator<T> validator) where T : IHasError
    {
        validator.RuleFor(x => x.LastError)
            .Empty()
            .WithMessage(x => $"Error: {x.LastError}");
    }

    public static async Task<ValidationResult> ValidateAsync<T>(this IValidator<T> validator, [NotNull] T instance, EditContext editContextRef, ValidationMessageStore validationMessageStore)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }
        
        //there are few problems with that abomination called validation system in AspNetCore/Blazor
        //1) ValidationMessages inherit EditContext using cascade parameters and generate field identifiers for the model inside that EditContext
        // there is possibility that the model which is validated will be different from the one that is a part of EditContext and this won't break anything
        //2) FieldIdentifiers for "For" in ValidationMessages DO NOT support actual expressions and tend to bind to the last model in the chain, e.g. Model1.Model2.Property => field identifier is for Model2 Property
        // which means you cannot even think about multi-level validations
        //3) Existing automated validation system is so fucking bad that I cannot imagine how to fit it into working product with async and responsive validations 
        
        validationMessageStore.Clear();
        var validationResult = await validator.ValidateAsync(instance);

        if (!ReferenceEquals(instance, editContextRef.Model))
        {
            Log.Warn($"Instance that is being validated differs from the one that is in EditContext: {instance} vs {editContextRef.Model}");
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        foreach (var validationFailure in validationResult.Errors)
        {
            var field = editContextRef.Field(validationFailure.PropertyName);
            validationMessageStore.Add(field, validationFailure.ErrorMessage);
        }
        editContextRef.NotifyValidationStateChanged();
        return validationResult;
    }
}