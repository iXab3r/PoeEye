using System;
using System.Linq.Expressions;
using System.Threading;
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
    
    /// <summary>
    /// Registers a DOM element as a file drop target within the WebView2-based Blazor UI.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element that should accept dropped files.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation of enabling the file drop target.
    /// </returns>
    /// <remarks>
    /// This method typically invokes a JavaScript function (e.g., <c>registerFileDropTarget</c>) to
    /// attach <c>dragover</c> and <c>drop</c> event listeners to the specified element. When files
    /// are dropped onto the element, they are posted to the WebView2 host via
    /// <c>window.chrome.webview.postMessageWithAdditionalObjects</c>.
    ///
    /// The host application can handle these messages through the WebView2
    /// <c>CoreWebView2.WebMessageReceived</c> event, receiving each file as a <c>CoreWebView2File</c>.
    ///
    /// Requires WebView2 SDK version 1.0.1518.46 or later, which supports <c>postMessageWithAdditionalObjects</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// await RegisterFileDropTarget(myElementRef);
    /// </code>
    /// </example>
    Task RegisterFileDropTarget(ElementReference elementRef);

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
    /// Sets CSS class in the element specified by a selector or DOM reference.
    /// </summary>
    /// <param name="elementRef">A reference to the HTML element.</param>
    /// <param name="classNames">One or more CSS class names</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetClass(ElementReference elementRef, params string[] classNames);
    
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
    
    /// <summary>
    /// Sets or removes an arbitrary attribute on a DOM element referenced by ElementReference.
    /// If <paramref name="value"/> is null, the attribute is removed; otherwise it is set to the provided value.
    /// </summary>
    /// <param name="elementRef">Reference to the target DOM element.</param>
    /// <param name="name">Attribute name (e.g., "data-cm-id").</param>
    /// <param name="value">Attribute value or null to remove the attribute.</param>
    Task SetAttribute(ElementReference elementRef, string name, string? value);

    /// <summary>
    /// Removes an arbitrary attribute from a DOM element referenced by ElementReference.
    /// </summary>
    /// <param name="elementRef">Reference to the target DOM element.</param>
    /// <param name="name">Attribute name to remove.</param>
    Task RemoveAttribute(ElementReference elementRef, string name);
    
    /// <summary>
    /// Returns the list of DOM elements present at the given viewport coordinates along with their breadcrumb paths
    /// (ancestors up to the document root). For each element and ancestor, includes tag, id, class list, and selected attributes.
    /// </summary>
    /// <param name="x">Viewport X coordinate (e.g., from mouse event clientX).</param>
    /// <param name="y">Viewport Y coordinate (e.g., from mouse event clientY).</param>
    /// <param name="maxDepth">Maximum number of elements from the hit-test stack to return. Use small numbers (e.g., 5–10).</param>
    /// <param name="attributeNames">List of attribute names to include, e.g., new[]{"data-cm-id"}.</param>
    /// <param name="cancellationToken">Cancellation token used to control async operation</param>
    /// <returns>Array of breadcrumb descriptors for each element under the point.</returns>
    Task<HtmlElementBreadcrumb[]> GetElementBreadcrumbsAt(double x, double y, int maxDepth, string[] attributeNames, CancellationToken cancellationToken = default);
}