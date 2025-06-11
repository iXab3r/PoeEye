import log from 'loglevel';

log.setLevel('info'); // Change this level as needed.


const keyDownListeners = new Map<HTMLElement, (event: KeyboardEvent) => void>();

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