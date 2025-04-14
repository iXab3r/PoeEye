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
