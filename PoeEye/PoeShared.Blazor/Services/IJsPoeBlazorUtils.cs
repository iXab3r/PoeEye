using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

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

    Task<IDynamicRootComponent> AddRootComponent(string elementId, string componentIdentifier, object initialParameters = default);
}