import log from 'loglevel';

log.setLevel('info'); // Change this level as needed.


const keyDownListeners = new Map<HTMLElement, (event: KeyboardEvent) => void>();

const defaultTitleBarInteractiveSelector = [
    'button',
    'input',
    'select',
    'textarea',
    'option',
    'a[href]',
    'summary',
    'label',
    '[role="button"]',
    '[role="link"]',
    '[role="menuitem"]',
    '[role="checkbox"]',
    '[role="radio"]',
    '[role="switch"]',
    '[role="tab"]',
    '[contenteditable]:not([contenteditable="false"])',
    '[data-blazor-window-titlebar-interactive]',
    '[data-no-window-drag]'
].join(',');

export interface BrowserShortcut {
    key: string;
    ctrlKey?: boolean;
    shiftKey?: boolean;
    altKey?: boolean;
    metaKey?: boolean;
}

export interface BrowserShortcutSuppressionOptions {
    shortcuts?: BrowserShortcut[];
    allowCtrlAInEditable?: boolean;
    allowSelector?: string;
    preventDefault?: boolean;
    stopPropagation?: boolean;
}

export interface BrowserShortcutSuppressionHandle {
    dispose(): void;
}

interface NormalizedBrowserShortcut {
    key: string;
    ctrlKey: boolean;
    shiftKey: boolean;
    altKey: boolean;
    metaKey: boolean;
}

interface NormalizedBrowserShortcutSuppressionOptions {
    shortcuts: NormalizedBrowserShortcut[];
    allowCtrlAInEditable: boolean;
    allowSelector: string;
    preventDefault: boolean;
    stopPropagation: boolean;
}

interface PendingTitleBarDrag {
    startClientX: number;
    startClientY: number;
}

type BrowserShortcutKeyboardEvent = Pick<KeyboardEvent, 'key' | 'ctrlKey' | 'shiftKey' | 'altKey' | 'metaKey' | 'defaultPrevented'> & {
    target?: unknown;
    composedPath?: () => EventTarget[];
};

type EventWithPath = {
    target?: unknown;
    composedPath?: () => EventTarget[];
};

type ElementLike = {
    tagName?: string;
    isContentEditable?: boolean;
    closest?: (selector: string) => unknown;
    parentElement?: unknown;
};

export const defaultBrowserShortcuts: BrowserShortcut[] = [
    { key: 'a', ctrlKey: true },
    { key: 'p', ctrlKey: true },
    { key: 'f', ctrlKey: true },
    { key: 's', ctrlKey: true },
    { key: 'o', ctrlKey: true },
    { key: 'r', ctrlKey: true },
    { key: 'r', ctrlKey: true, shiftKey: true },
    { key: 'F5' }
];

/**
 * Installs a keyboard hook to listen for keydown events on the specified HTMLElement and invoke a Blazor method.
 * @param element The HTML element to attach the keydown listener to.
 * @param dotNetReference A reference to the Blazor component for callback.
 * @param methodName The method name in the Blazor component to invoke on keydown.
 */
export function addKeyboardHook(element: HTMLElement, dotNetReference: DotNet.DotNetObject, methodName: string): void {
    const eventHandler = async (event: KeyboardEvent) => {
        const keyboardEventArgs: IKeyboardEventArgs = {
            key: event.key,
            code: event.code,
            location: event.location,
            repeat: event.repeat,
            ctrlKey: event.ctrlKey,
            shiftKey: event.shiftKey,
            altKey: event.altKey,
            metaKey: event.metaKey,
            type: event.type
        };
        const handled = await dotNetReference.invokeMethodAsync<boolean>(methodName, keyboardEventArgs);
        log.info(`Key press handled: ${event.key} = ${handled}`);
        if (handled) {
            event.preventDefault();
            event.stopPropagation();
        }
    };

    element.addEventListener('keydown', eventHandler);
    keyDownListeners.set(element, eventHandler);
}

export function suppressWellKnownBrowserShortcuts(options?: BrowserShortcutSuppressionOptions): BrowserShortcutSuppressionHandle {
    const effectiveOptions = normalizeBrowserShortcutSuppressionOptions(options);
    const eventHandler = (event: KeyboardEvent) => {
        if (!shouldSuppressBrowserShortcut(event, effectiveOptions)) {
            return;
        }

        if (effectiveOptions.preventDefault) {
            event.preventDefault();
        }

        if (effectiveOptions.stopPropagation) {
            event.stopPropagation();
        }
    };

    document.addEventListener('keydown', eventHandler, true);
    return {
        dispose: () => document.removeEventListener('keydown', eventHandler, true)
    };
}

export function registerWindowTitleBarGestures(
    element: HTMLElement,
    dotNetReference: DotNet.DotNetObject,
    dragThresholdPixels?: number): Disposable {

    if (!(element instanceof HTMLElement)) {
        throw new Error('Titlebar element is not an HTMLElement');
    }

    const effectiveDragThresholdPixels = Math.max(1, dragThresholdPixels ?? 4);
    let pendingDrag: PendingTitleBarDrag | null = null;
    let disposed = false;

    const clearPendingDrag = () => {
        pendingDrag = null;
        document.removeEventListener('mousemove', handleDocumentMouseMove, true);
        document.removeEventListener('mouseup', handleDocumentMouseUp, true);
    };

    const beginPotentialDrag = (event: MouseEvent) => {
        if (disposed || event.button !== 0 || !isTitleBarSurfaceEvent(event, element)) {
            return;
        }

        pendingDrag = {
            startClientX: event.clientX,
            startClientY: event.clientY
        };
        document.addEventListener('mousemove', handleDocumentMouseMove, true);
        document.addEventListener('mouseup', handleDocumentMouseUp, true);
    };

    function handleDocumentMouseMove(event: MouseEvent) {
        if (pendingDrag == null) {
            clearPendingDrag();
            return;
        }

        if ((event.buttons & 1) !== 1) {
            clearPendingDrag();
            return;
        }

        const deltaX = Math.abs(event.clientX - pendingDrag.startClientX);
        const deltaY = Math.abs(event.clientY - pendingDrag.startClientY);
        if (deltaX < effectiveDragThresholdPixels && deltaY < effectiveDragThresholdPixels) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        clearPendingDrag();
        dotNetReference.invokeMethodAsync('HandleTitleBarDragStart')
            .catch(error => log.warn('Failed to start titlebar drag', error));
    }

    function handleDocumentMouseUp() {
        clearPendingDrag();
    }

    const handleDoubleClick = (event: MouseEvent) => {
        if (disposed || event.button !== 0 || !isTitleBarSurfaceEvent(event, element)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        clearPendingDrag();
        dotNetReference.invokeMethodAsync('HandleTitleBarDoubleClick')
            .catch(error => log.warn('Failed to handle titlebar double click', error));
    };

    const handleContextMenu = (event: MouseEvent) => {
        if (disposed || !isTitleBarSurfaceEvent(event, element)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        clearPendingDrag();
        dotNetReference.invokeMethodAsync('HandleTitleBarContextMenu')
            .catch(error => log.warn('Failed to handle titlebar context menu', error));
    };

    element.addEventListener('mousedown', beginPotentialDrag, true);
    element.addEventListener('dblclick', handleDoubleClick, true);
    element.addEventListener('contextmenu', handleContextMenu, true);

    return {
        dispose: () => {
            disposed = true;
            clearPendingDrag();
            element.removeEventListener('mousedown', beginPotentialDrag, true);
            element.removeEventListener('dblclick', handleDoubleClick, true);
            element.removeEventListener('contextmenu', handleContextMenu, true);
        }
    };
}

export function shouldSuppressBrowserShortcut(event: BrowserShortcutKeyboardEvent, options?: BrowserShortcutSuppressionOptions): boolean {
    const effectiveOptions = normalizeBrowserShortcutSuppressionOptions(options);
    if (event.defaultPrevented) {
        return false;
    }

    const eventTargets = getEventTargets(event);
    if (effectiveOptions.allowSelector && eventTargets.some(target => isInsideAllowedShortcutSubtree(target, effectiveOptions.allowSelector))) {
        return false;
    }

    const shortcut = findMatchingShortcut(event, effectiveOptions.shortcuts);
    if (shortcut == null) {
        return false;
    }

    if (effectiveOptions.allowCtrlAInEditable && isPlainCtrlA(shortcut) && eventTargets.some(isEditableTarget)) {
        return false;
    }

    return true;
}

function normalizeBrowserShortcutSuppressionOptions(options?: BrowserShortcutSuppressionOptions): NormalizedBrowserShortcutSuppressionOptions {
    return {
        shortcuts: (options?.shortcuts ?? defaultBrowserShortcuts).map(normalizeBrowserShortcut),
        allowCtrlAInEditable: options?.allowCtrlAInEditable ?? true,
        allowSelector: options?.allowSelector ?? '[data-poe-browser-shortcuts="allow"]',
        preventDefault: options?.preventDefault ?? true,
        stopPropagation: options?.stopPropagation ?? false
    };
}

function normalizeBrowserShortcut(shortcut: BrowserShortcut): NormalizedBrowserShortcut {
    return {
        key: normalizeKeyboardKey(shortcut.key),
        ctrlKey: shortcut.ctrlKey ?? false,
        shiftKey: shortcut.shiftKey ?? false,
        altKey: shortcut.altKey ?? false,
        metaKey: shortcut.metaKey ?? false
    };
}

function normalizeKeyboardKey(key: string): string {
    return key.trim().toLowerCase();
}

function findMatchingShortcut(
    event: Pick<KeyboardEvent, 'key' | 'ctrlKey' | 'shiftKey' | 'altKey' | 'metaKey'>,
    shortcuts: NormalizedBrowserShortcut[]): NormalizedBrowserShortcut | undefined {
    const eventKey = normalizeKeyboardKey(event.key);
    return shortcuts.find(shortcut =>
        shortcut.key === eventKey &&
        shortcut.ctrlKey === event.ctrlKey &&
        shortcut.shiftKey === event.shiftKey &&
        shortcut.altKey === event.altKey &&
        shortcut.metaKey === event.metaKey);
}

function isPlainCtrlA(shortcut: NormalizedBrowserShortcut): boolean {
    return shortcut.key === 'a' &&
        shortcut.ctrlKey &&
        !shortcut.shiftKey &&
        !shortcut.altKey &&
        !shortcut.metaKey;
}

function isEditableTarget(target: unknown): boolean {
    const element = getElementLike(target);
    if (element == null) {
        return false;
    }

    if (element.isContentEditable) {
        return true;
    }

    const tagName = (element.tagName ?? '').toLowerCase();
    if (tagName === 'input' || tagName === 'textarea' || tagName === 'select') {
        return true;
    }

    const editableHost = getElementLike(safeClosest(element, '[contenteditable]'));
    return !!editableHost?.isContentEditable;
}

function isInsideAllowedShortcutSubtree(target: unknown, selector: string): boolean {
    const element = getElementLike(target);
    return element != null && !!safeClosest(element, selector);
}

function getEventTargets(event: EventWithPath): unknown[] {
    const composedPath = typeof event.composedPath === 'function' ? event.composedPath() : undefined;
    if (composedPath != null && composedPath.length > 0) {
        return composedPath;
    }

    return [event.target];
}

function getElementLike(target: unknown): ElementLike | null {
    if (target == null || typeof target !== 'object') {
        return null;
    }

    const candidate = target as ElementLike;
    if (typeof candidate.closest === 'function' || candidate.tagName != null || candidate.isContentEditable != null) {
        return candidate;
    }

    return getElementLike(candidate.parentElement);
}

function safeClosest(element: { closest?: (selector: string) => unknown }, selector: string): unknown {
    if (typeof element.closest !== 'function') {
        return null;
    }

    try {
        return element.closest(selector);
    } catch (error) {
        log.warn(`Invalid shortcut suppression selector '${selector}'`, error);
        return null;
    }
}

function isTitleBarSurfaceEvent(
    event: MouseEvent,
    titleBarElement: HTMLElement): boolean {

    const eventTargets = getEventTargets(event);
    for (const target of eventTargets) {
        if (target === titleBarElement) {
            return true;
        }

        const element = getElementLike(target);
        if (element == null) {
            continue;
        }

        if (isInteractiveTitleBarElement(element)) {
            return false;
        }
    }

    return titleBarElement.contains(event.target as Node | null);
}

function isInteractiveTitleBarElement(element: ElementLike): boolean {
    if (element.isContentEditable) {
        return true;
    }

    return !!safeClosest(element, defaultTitleBarInteractiveSelector);
}

export function createElementInputManager(element: HTMLElement) : ElementInputEventManager{
    return new ElementInputEventManager(element);
}

// Define an interface that matches the structure of Blazor's KeyboardEventArgs
interface IKeyboardEventArgs {
    key: string;
    code: string;
    location: number;
    repeat: boolean;
    ctrlKey: boolean;
    shiftKey: boolean;
    altKey: boolean;
    metaKey: boolean;
    type: string;
}

interface IHotkeyGesture {
    key: string;
    ctrlKey: boolean;
    shiftKey: boolean;
    altKey: boolean;
    metaKey: boolean;
}

interface IElementInputEventManager extends Disposable {
    
}

class ElementInputEventManager implements Disposable {
    
    private readonly target: HTMLElement;

    constructor(target: HTMLElement) {
        this.target = target;
    }

    [Symbol.dispose](): void {
        throw new Error('Method not implemented.');
    }
}
