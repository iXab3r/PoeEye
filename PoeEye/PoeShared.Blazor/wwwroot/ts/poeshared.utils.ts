import swal from 'sweetalert';
import log from 'loglevel';
import $ from 'jquery'; // Ensure jQuery is imported

log.setLevel('info'); // Change this level as needed.

/**
 * Logs an error message and throws an error.
 * @param errorMessage The error message to log and throw.
 * @param error (Optional) The error object to include in the message.
 * @throws An error with the specified error message.
 */
export function logAndThrow(errorMessage: string, error?: Error): never {
    if (error) {
        errorMessage += `: ${error.message}`;
    }
    log.error(errorMessage);
    throw new Error(errorMessage);
}

/**
 * Reads text from the clipboard using the Clipboard API.
 * @returns A Promise that resolves to the clipboard text.
 * @throws An error if reading from the clipboard fails.
 */
export async function getClipboardText(): Promise<string> {
    try {
        return await navigator.clipboard.readText();
    } catch (error) {
        logAndThrow('Failed to read clipboard contents', error);
    }
}

/**
 * Writes text to the clipboard using the Clipboard API.
 * @param text The text to copy to the clipboard.
 * @throws An error if copying text to the clipboard fails.
 */
export async function setClipboardText(text: string): Promise<void> {
    try {
        await navigator.clipboard.writeText(text);
    } catch (error) {
        logAndThrow('Failed to copy text to clipboard', error);
    }
}

/**
 * Displays a text alert using a custom alert library.
 * @param message The message to display in the alert.
 */
export async function showAlert(message: string) {
    log.debug(`Showing alert: ${message}`);
    await swal(message);
}

/**
 * Focuses on an HTML element with the specified ID.
 * @param elementId The ID of the element to focus on.
 * @throws An error if the element is not found or if focusing fails.
 */
export function focusElementById<T extends HTMLElement>(elementId: string): void {
    const targetElement = document.getElementById(elementId) as T | null;

    if (!targetElement) {
        logAndThrow(`Element with ID '${elementId}' not found`);
    }

    try {
        targetElement.focus();
    } catch (error) {
        logAndThrow(`Error focusing on element with ID '${elementId}'`, error);
    }
}

/**
 * Selects all text in an HTML input element with the specified ID.
 * @param elementId The ID of the input element to select text in.
 * @throws An error if the element is not found, is not an input element, or if text selection fails.
 */
export function selectAllTextInElementById<T extends HTMLElement>(elementId: string): void {
    const targetElement = document.getElementById(elementId) as T | null;

    if (!targetElement) {
        logAndThrow(`Element with ID '${elementId}' not found`);
    }

    if (!(targetElement instanceof HTMLInputElement)) {
        logAndThrow(`Element with ID '${elementId}' is not an input element`);
    }

    try {
        targetElement.select();
    } catch (error) {
        logAndThrow(`Error selecting text in element with ID '${elementId}'`, error);
    }
}

/**
 * Simulates a click event on an HTML element with the specified ID.
 * @param elementId The ID of the element to click.
 * @throws An error if the element is not found, is not an HTMLElement, or does not support the 'click' method.
 */
export function clickElementById(elementId: string): void {
    const targetElement = document.getElementById(elementId);

    if (!targetElement) {
        logAndThrow(`Element with ID '${elementId}' not found`);
    }

    if (!(targetElement instanceof HTMLElement)) {
        logAndThrow(`Element with ID '${elementId}' is not an HTMLElement`);
    }

    if (typeof targetElement.click !== 'function') {
        logAndThrow(`Element with ID '${elementId}' does not support the 'click' method`);
    }

    try {
        targetElement.click();
    } catch (error) {
        logAndThrow(`Error clicking element with ID '${elementId}'`, error);
    }
}

/**
 * Scrolls to the top of an element matched by the specified jQuery selector.
 * @param selector The jQuery selector to identify the element to scroll.
 * @param speed Optional. The duration of the scroll animation in milliseconds.
 * @throws An error if the element is not found or if there's an error during scrolling.
 */
export function scrollToTop(selector: string, speed: number = 600): void {
    const targetElement = $(selector);

    if (targetElement.length === 0) {
        logAndThrow(`Element with selector '${selector}' not found`);
    }

    try {
        targetElement.animate({ scrollTop: 0 }, speed);
    } catch (error) {
        logAndThrow(`Error scrolling to top of element with selector '${selector}'`, error);
    }
}

/**
 * Scrolls to the bottom of an element matched by the specified jQuery selector.
 * @param selector The jQuery selector to identify the element to scroll.
 * @param speed Optional. The duration of the scroll animation in milliseconds.
 * @throws An error if the element is not found or if there's an error during scrolling.
 */
export function scrollToBottom(selector: string, speed: number = 600): void {
    const targetElement = $(selector);

    if (targetElement.length === 0) {
        logAndThrow(`Element with selector '${selector}' not found`);
    }

    try {
        const height = targetElement[0].scrollHeight;
        targetElement.animate({ scrollTop: height }, speed);
    } catch (error) {
        logAndThrow(`Error scrolling to bottom of element with selector '${selector}'`, error);
    }
}
