﻿using System;
using System.Linq.Expressions;
using PoeShared.Scaffolding;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Components;
using ReactiveUI.Validation.Helpers;
using ReactiveUI.Validation.TemplateGenerators;

namespace PoeShared.Wpf.Scaffolding;

/// <summary>
/// Extensions methods associated to <see cref="IValidatableViewModel"/> instances.
/// </summary>
public static class SupportsValidationExtensions
{
    /// <summary>
    /// Setup a validation rule for a specified ViewModel property with static error message.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    /// <typeparam name="TViewModelProp">ViewModel property type.</typeparam>
    /// <param name="viewModel">ViewModel instance.</param>
    /// <param name="viewModelProperty">ViewModel property.</param>
    /// <param name="isPropertyValid">Func to define if the viewModelProperty is valid or not.</param>
    /// <param name="message">Validation error message.</param>
    /// <returns>Returns a <see cref="ValidationHelper"/> object.</returns>
    public static ValidationHelper ValidationRule<TViewModel, TViewModelProp>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TViewModelProp>> viewModelProperty,
        Func<TViewModelProp, bool> isPropertyValid,
        string message)
        where TViewModel : DisposableReactiveObject, IValidatableViewModel
    {
        // We need to associate the ViewModel property
        // with something that can be easily looked up and bound to
        var propValidation = new BasePropertyValidation<TViewModel, TViewModelProp>(
            viewModel,
            viewModelProperty,
            isPropertyValid,
            message);

        viewModel.ValidationContext.Add(propValidation);

        return new ValidationHelper(propValidation);
    }

    /// <summary>
    /// Setup a validation rule for a specified ViewModel property with dynamic error message.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    /// <typeparam name="TViewModelProp">ViewModel property type.</typeparam>
    /// <param name="viewModel">ViewModel instance.</param>
    /// <param name="viewModelProperty">ViewModel property.</param>
    /// <param name="isPropertyValid">Func to define if the viewModelProperty is valid or not.</param>
    /// <param name="message">Func to define the validation error message based on the viewModelProperty value.</param>
    /// <returns>Returns a <see cref="ValidationHelper"/> object.</returns>
    public static ValidationHelper ValidationRule<TViewModel, TViewModelProp>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TViewModelProp>> viewModelProperty,
        Func<TViewModelProp, bool> isPropertyValid,
        Func<TViewModelProp, string> message)
        where TViewModel : DisposableReactiveObject, IValidatableViewModel
    {
        // We need to associate the ViewModel property
        // with something that can be easily looked up and bound to
        var propValidation = new BasePropertyValidation<TViewModel, TViewModelProp>(
            viewModel,
            viewModelProperty,
            isPropertyValid,
            message);
            
        viewModel.ValidationContext.Add(propValidation);

        return new ValidationHelper(propValidation);
    }
        
    public static ValidationHelper ValidationRule<TViewModel, TViewModelProp1,TViewModelProp2>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TViewModelProp1>> viewModelProperty1,
        Expression<Func<TViewModel, TViewModelProp2>> viewModelProperty2,
        Func<(TViewModelProp1, TViewModelProp2),  bool> isPropertyValid,
        Func<(TViewModelProp1, TViewModelProp2), string> message)
        where TViewModel : DisposableReactiveObject, IValidatableViewModel
    {
        // We need to associate the ViewModel property
        // with something that can be easily looked up and bound to
        var propValidation = new BasePropertyValidation<TViewModel, TViewModelProp1, TViewModelProp2>(
            viewModel,
            viewModelProperty1,
            viewModelProperty2,
            isPropertyValid,
            message);

        viewModel.ValidationContext.Add(propValidation);

        return new ValidationHelper(propValidation);
    }

    /// <summary>
    /// Setup a validation rule with a general observable indicating validity.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    /// <param name="viewModel">ViewModel instance.</param>
    /// <param name="viewModelObservableProperty">Func to define if the viewModel is valid or not.</param>
    /// <param name="messageFunc">Func to define the validation error message based on the viewModel and viewModelObservableProperty values.</param>
    /// <returns>Returns a <see cref="ValidationHelper"/> object.</returns>
    /// <remarks>
    /// It should be noted that the observable should provide an initial value, otherwise that can result
    /// in an inconsistent performance.
    /// </remarks>
    public static ValidationHelper ValidationRule<TViewModel>(
        this TViewModel viewModel,
        Func<TViewModel, IObservable<bool>> viewModelObservableProperty,
        Func<TViewModel, bool, string> messageFunc)
        where TViewModel : DisposableReactiveObject, IValidatableViewModel
    {
        var validation =
            new ModelObservableValidation<TViewModel>(viewModel, viewModelObservableProperty, messageFunc);

        viewModel.ValidationContext.Add(validation);

        return new ValidationHelper(validation);
    }

    /// <summary>
    /// Gets an observable for the validity of the ViewModel.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    /// <param name="viewModel">ViewModel instance.</param>
    /// <returns>Returns true if the ValidationContext is valid, otherwise false.</returns>
    public static IObservable<bool> IsValid<TViewModel>(this TViewModel viewModel)
        where TViewModel : DisposableReactiveObject, IValidatableViewModel
    {
        return viewModel?.ValidationContext.Valid;
    }
}