import log from 'loglevel';

export const Blazor:IBlazor = window['Blazor'];

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

export type ComponentParameters = object | null | undefined;

export interface IDynamicRootComponent {
    setParameters(parameters: ComponentParameters): void;
    dispose(): Promise<void>;
}

export interface IRootComponentsFunctions {
    add(toElement: Element, componentIdentifier: string, initialParameters: ComponentParameters): Promise<IDynamicRootComponent>;
}

export interface IBlazor {
    rootComponents: IRootComponentsFunctions;
}


/**
 * Interface that defines the useful methods on the .NET object reference passed by Blazor.
 */
export interface IBlazorInteropObject {
    invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
    invokeMethod<T>(methodName: string, ...args: any[]): T;
}

export interface CoreWebView2 {
    postMessage(message: any): void;
    postMessageWithAdditionalObjects(message: any, additionalObjects: ArrayLike<any>): void;
    addEventListener(
        type: "message",
        listener: (event: { data: any; additionalObjects?: any[] }) => void
    ): void;
    removeEventListener(
        type: "message",
        listener: (event: { data: any; additionalObjects?: any[] }) => void
    ): void;
}

export interface ChromeWithWebView {
    webview: CoreWebView2;
}

export interface ChromeWindow {
    chrome?: ChromeWithWebView;
}

export function getChromeWebView2(): CoreWebView2 | null {
    const maybeChrome = (window as any).chrome;

    if (!maybeChrome || typeof maybeChrome !== "object") {
        console.warn("window.chrome is not available or not an object.");
        return null;
    }

    const maybeWebView = maybeChrome.webview;

    if (!maybeWebView || typeof maybeWebView.postMessage !== "function") {
        console.warn("chrome.webview is not available or does not implement postMessage.");
        return null;
    }

    return maybeWebView as CoreWebView2;
}
