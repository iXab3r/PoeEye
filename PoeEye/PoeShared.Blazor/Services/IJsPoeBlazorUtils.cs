using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Blazor.Services;

/// <summary>
/// Represents a utility interface for JavaScript operations in a Blazor application.
/// </summary>
public interface IJsPoeBlazorUtils : IAsyncDisposable
{
    /// <summary>
    /// Gets the text from the clipboard asynchronously.
    /// </summary>
    /// <returns>The text from the clipboard.</returns>
    Task<string> GetClipboardText(); 
    
    /// <summary>
    /// Sets the text in the clipboard.
    /// </summary>
    /// <param name="text">The text that needs to be set in the clipboard.</param>
    /// <returns>An awaitable Task that can be used for continuation when the clipboard operation is done.</returns>
    Task SetClipboardText(string text);

    /// <summary>
    /// Shows a text alert using SweetAlert.
    /// </summary>
    /// <param name="message">The message to display in the alert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowAlert(string message);

    /// <summary>
    /// Selects all text within an HTML element identified by its ID.
    /// </summary>
    /// <param name="elementId">The ID of the HTML element to select text in.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SelectAllTextInElementById(string elementId);

    /// <summary>
    /// Selects all text within an HTML element identified by its ID.
    /// </summary>
    /// <param name="elementRef"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SelectAllTextInElement(ElementReference elementRef);
    
    /// <summary>
    /// Scrolls the specified HTML element into view.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element to scroll into view.</param>
    /// <param name="behavior">Defines the transition animation. Can be 'auto' or 'smooth'.</param>
    /// <param name="block">Defines vertical alignment. Can be 'start', 'center', 'end', or 'nearest'.</param>
    /// <param name="inline">Defines horizontal alignment. Can be 'start', 'center', 'end', or 'nearest'.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ScrollElementIntoView(ElementReference elementRef, string behavior = "smooth", string block = "nearest", string inline = "nearest");

    /// <summary>
    /// Selects text range within an HTML element identified by its ID.
    /// </summary>
    /// <param name="elementRef"></param>
    /// <param name="end">The offset into the text field for the end of the selection.</param>
    /// <param name="direction">The direction in which the selection is performed.</param>
    /// <param name="start">The offset into the text field for the start of the selection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SelectTextRangeInElement(ElementReference elementRef, int? start = default, int? end = default, JsSelectionRangeDirection direction = JsSelectionRangeDirection.Forward);

    /// <summary>
    /// Sets focus on the element with the provided id.
    /// </summary>
    /// <param name="elementId">The id of the element to gain focus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FocusElementById(string elementId);
    
    /// <summary>
    /// Clicks on the element with the provided id.
    /// </summary>
    /// <param name="elementId">The id of the element to be clicked on.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ClickElementById(string elementId);
    
    /// <summary>
    /// Scrolls to the top of an element matching specified selector using jQuery.
    /// </summary>
    Task ScrollToTop(string elementId, TimeSpan duration);
    
    /// <summary>
    /// Scrolls to the top of an element matching specified selector using jQuery.
    /// </summary>
    Task ScrollToTop(string elementSelector);
    
    /// <summary>
    /// Scrolls to the bottom of an element matching specified selector using jQuery.
    /// </summary>
    Task ScrollToBottom(string elementSelector, TimeSpan duration);
    
    /// <summary>
    /// Scrolls to the bottom of an element matching specified selector using jQuery.
    /// </summary>
    Task ScrollToBottom(string elementSelector);
    
    /// <summary>
    /// Loads a JavaScript file dynamically and returns a promise that completes when the script loads.
    /// </summary>
    Task LoadScript(string scriptPath);
    
    /// <summary>
    /// Loads CSS file dynamically
    /// </summary>
    Task LoadCss(string cssPath);

    Task<IDynamicRootComponent> AddRootComponent(string elementId, string componentIdentifier, object initialParameters = default);

    Task AddKeyboardHook<THandler>(
        ElementReference elementRef, 
        DotNetObjectReference<THandler> dotNetObjectReference, 
        string methodName) where THandler : class;
    
    Task<ElementKeyboardHookRef> AddKeyboardHook<THandler>(
        ElementReference elementRef,
        THandler handler, 
        string methodName) where THandler : class;
    
    Task RemoveKeyboardHook(ElementReference elementRef);
    
    /// <summary>
    /// Adds one or more CSS classes to the element specified by a selector or DOM reference.
    /// </summary>
    /// <param name="selectorOrElement">A CSS selector (e.g., "#myElement") or an <see cref="ElementReference"/> representing the target element.</param>
    /// <param name="classNames">One or more CSS class names to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddClass(string selectorOrElement, params string[] classNames);

    /// <summary>
    /// Removes one or more CSS classes from the element specified by a selector or DOM reference.
    /// </summary>
    /// <param name="selectorOrElement">A CSS selector (e.g., ".active") or an <see cref="ElementReference"/> representing the target element.</param>
    /// <param name="classNames">One or more CSS class names to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveClass(string selectorOrElement, params string[] classNames);

    /// <summary>
    /// Toggles the presence of a CSS class on the target element.
    /// </summary>
    /// <param name="selectorOrElement">A CSS selector or <see cref="ElementReference"/> targeting the DOM element.</param>
    /// <param name="className">The class name to toggle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ToggleClass(string selectorOrElement, string className);

    /// <summary>
    /// Checks whether a DOM element has a specific CSS class.
    /// </summary>
    /// <param name="selectorOrElement">A CSS selector or <see cref="ElementReference"/> representing the element.</param>
    /// <param name="className">The class name to check for.</param>
    /// <returns>A task that resolves to <c>true</c> if the element has the class; otherwise, <c>false</c>.</returns>
    Task<bool> HasClass(string selectorOrElement, string className);
    
    /// <summary>
    /// Adds one or more CSS classes to the element specified by a selector or DOM reference.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element.</param>
    /// <param name="classNames">One or more CSS class names to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddClass(ElementReference elementRef, params string[] classNames);

    /// <summary>
    /// Removes one or more CSS classes from the element specified by a selector or DOM reference.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element.</param>
    /// <param name="classNames">One or more CSS class names to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveClass(ElementReference elementRef, params string[] classNames);

    /// <summary>
    /// Toggles the presence of a CSS class on the target element.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element.</param>
    /// <param name="className">The class name to toggle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ToggleClass(ElementReference elementRef, string className);

    /// <summary>
    /// Checks whether a DOM element has a specific CSS class.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element.</param>
    /// <param name="className">The class name to check for.</param>
    /// <returns>A task that resolves to <c>true</c> if the element has the class; otherwise, <c>false</c>.</returns>
    Task<bool> HasClass(ElementReference elementRef, string className);
}