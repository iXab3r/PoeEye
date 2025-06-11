export * from './blazorUtils.input'
export * from './blazorUtils.loader'
export * from './blazorUtils.dragndrop'

import swal from 'sweetalert';
import log from 'loglevel';
import $ from 'jquery';

// noinspection ES6UnusedImports used from outside
import {ComponentParameters, IDynamicRootComponent, IBlazor, Blazor, logAndThrow} from "./blazorUtils.common";

log.setLevel('info'); // Change this level as needed.

/**
 * Sets the class list of an element, replacing any existing classes.
 * @param target A selector string or HTMLElement.
 * @param classNames A single class or array of classes to set.
 */
export function setClass(target: string | HTMLElement, classNames: string | string[]): void {
    const element = resolveElement(target);
    const classes = normalizeClassNames(classNames);

    try {
        element.className = classes.join(' ');
    } catch (error) {
        logAndThrow(`Error setting class(es) '${classes.join(', ')}' on element`, error);
    }
}

/**
 * Adds one or more classes to an element.
 * @param target A selector string or HTMLElement.
 * @param classNames A single class or array of classes to add.
 */
export function addClass(target: string | HTMLElement, classNames: string | string[]): void {
    const element = resolveElement(target);
    const classes = normalizeClassNames(classNames);

    try {
        element.classList.add(...classes);
    } catch (error) {
        logAndThrow(`Error adding class(es) '${classes.join(', ')}' to element`, error);
    }
}

/**
 * Removes one or more classes from an element.
 * @param target A selector string or HTMLElement.
 * @param classNames A single class or array of classes to remove.
 */
export function removeClass(target: string | HTMLElement, classNames: string | string[]): void {
    const element = resolveElement(target);
    const classes = normalizeClassNames(classNames);

    try {
        element.classList.remove(...classes);
    } catch (error) {
        logAndThrow(`Error removing class(es) '${classes.join(', ')}' from element`, error);
    }
}

/**
 * Toggles a single class on an element.
 * @param target A selector string or HTMLElement.
 * @param className The class to toggle.
 */
export function toggleClass(target: string | HTMLElement, className: string): void {
    const element = resolveElement(target);
    const classTrimmed = className.trim();

    if (!classTrimmed) {
        logAndThrow(`Invalid class name '${className}'`);
    }

    try {
        element.classList.toggle(classTrimmed);
    } catch (error) {
        logAndThrow(`Error toggling class '${classTrimmed}' on element`, error);
    }
}

/**
 * Checks if the element has the specified class.
 * @param target A selector string or HTMLElement.
 * @param className The class to check for.
 * @returns True if the element has the class, false otherwise.
 */
export function hasClass(target: string | HTMLElement, className: string): boolean {
    const element = resolveElement(target);
    const classTrimmed = className.trim();

    if (!classTrimmed) {
        logAndThrow(`Invalid class name '${className}'`);
    }

    try {
        return element.classList.contains(classTrimmed);
    } catch (error) {
        logAndThrow(`Error checking class '${classTrimmed}' on element`, error);
    }
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

    selectAllTextInElement(targetElement);
}

/**
 * Selects all text in an HTML input element with the specified ID.
 * @throws An error if the element is not found, is not an input element, or if text selection fails.
 * @param targetElement
 */
export function selectAllTextInElement(targetElement: HTMLElement): void {
    if (!targetElement) {
        logAndThrow(`Element not specified`);
    }

    if (!(targetElement instanceof HTMLInputElement)) {
        logAndThrow(`Element '${targetElement}' is not an input element`);
    }

    try {
        targetElement.select();
    } catch (error) {
        logAndThrow(`Error selecting text in element ${targetElement}`, error);
    }
}

/**
 * Selects text range in an HTML input element with the specified ID.
 * @throws An error if the element is not found, is not an input element, or if text selection fails.
 * @param targetElement
 * @param start The offset into the text field for the start of the selection.
 * @param end The offset into the text field for the end of the selection.
 * @param direction The direction in which the selection is performed.
 */
export function selectTextRangeInElement(targetElement: HTMLElement, start: number | null, end: number | null, direction?: "forward" | "backward" | "none"): void {
    if (!targetElement) {
        logAndThrow(`Element not specified`);
    }

    if (!(targetElement instanceof HTMLInputElement)) {
        logAndThrow(`Element '${targetElement}' is not an input element`);
    }

    try {
        targetElement.setSelectionRange(start, end, direction);
    } catch (error) {
        logAndThrow(`Error selecting text in element ${targetElement}`, error);
    }
}

/**
 * Scrolls the specified HTML element into view.
 * @throws An error if the element is not found or the scroll operation fails.
 * @param targetElement The HTML element to scroll into view.
 * @param behavior Defines the transition animation. Can be 'auto' or 'smooth'.
 * @param block Defines vertical alignment. Can be 'start', 'center', 'end', or 'nearest'.
 * @param inline Defines horizontal alignment. Can be 'start', 'center', 'end', or 'nearest'.
 */
export function scrollElementIntoView(targetElement: HTMLElement, behavior: "auto" | "smooth" = "smooth", block: "start" | "center" | "end" | "nearest" = "nearest", inline: "start" | "center" | "end" | "nearest" = "nearest"): void {
    if (!targetElement) {
        logAndThrow(`Element not specified`);
    }

    try {
        targetElement.scrollIntoView({ behavior: behavior, block: block, inline: inline });
    } catch (error) {
        logAndThrow(`Error scrolling element into view`, error);
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

/**
 * Instantiated new custom component in specified HTML element by Id
 * @param elementId The ID of the element which will be used as container
 * @param componentIdentifier Identifier of blazor custom element
 * @param initialParameters initial component parameters
 * @throws An error if the element is not found or component failed to create
 */
export async function addRootComponent<T extends HTMLElement>(elementId: string, componentIdentifier: string, initialParameters: ComponentParameters): Promise<IDynamicRootComponent> {
    const targetElement = getElementByIdOrThrow<T>(elementId);

    try {
        const parameters = initialParameters === undefined || initialParameters === null ? {} : initialParameters;
        return await Blazor.rootComponents.add(targetElement, componentIdentifier, parameters);
    } catch (error) {
        logAndThrow(`Error focusing on element with ID '${elementId}'`, error);
    }
}

function getElementByIdOrThrow<T extends HTMLElement>(elementId: string): T{
    const targetElement = document.getElementById(elementId) as T | null;

    if (!targetElement) {
        logAndThrow(`Element with ID '${elementId}' not found`);
    }

    return targetElement;
}

/**
 * Normalizes the input into an HTMLElement or throws if not found.
 * @param target A selector string or an HTMLElement.
 * @returns The resolved HTMLElement.
 */
function resolveElement(target: string | HTMLElement): HTMLElement {
    if (typeof target === 'string') {
        const element = document.querySelector<HTMLElement>(target);
        if (!element) {
            logAndThrow(`Element with selector '${target}' not found`);
        }
        return element;
    } else if (target instanceof HTMLElement) {
        return target;
    } else {
        logAndThrow(`Invalid target: must be a selector string or HTMLElement`);
    }
}

/**
 * Ensures the classNames input is always an array of non-empty strings.
 */
function normalizeClassNames(classNames: string | string[]): string[] {
    const classes = Array.isArray(classNames) ? classNames : [classNames];
    return classes.map(c => c.trim()).filter(c => c.length > 0);
}

/**
 * Attempts to find an element by its ID in the DOM with a delay and a timeout.
 * @param elementId The ID of the element to find.
 * @param timeout The maximum amount of time to wait for the element (in milliseconds). Default is 1000ms.
 * @param delay The interval between attempts to find the element (in milliseconds). Default is 50ms.
 * @returns A Promise that resolves to the found HTMLElement.
 * @throws Will throw an error if the element is not found within the specified timeout.
 */
export function getElementByIdWithDelay(
    elementId: string,
    timeout: number = 10000,
    delay: number = 50
): Promise<HTMLElement> {
    const startTime = Date.now();

    return new Promise((resolve, reject) => {
        function attemptToFindElement() {
            const elapsedTime = Date.now() - startTime;
            if (elapsedTime > timeout) {
                // Timeout reached, throw an error
                reject(new Error(`Element with ID '${elementId}' not found within ${timeout}ms`));
                return;
            }

            const element = document.getElementById(elementId);
            if (element) {
                // Element found, resolve the promise
                resolve(element);
            } else {
                // Element not found, try again after a delay
                setTimeout(attemptToFindElement, delay);
            }
        }

        attemptToFindElement();
    });
}
