// noinspection JSUnusedGlobalSymbols called from .NET

import log from 'loglevel';

log.setLevel('info'); 

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

export const BlazorFacade:IBlazor = window['Blazor'];
if (!BlazorFacade) {
    logAndThrow(`Blazor seems to not be attached to Window yet`);
}

/**
 * Instantiated new custom component in specified HTML element by Id
 * @param targetElement
 * @param componentIdentifier Identifier of blazor custom element
 * @param initialParameters initial component parameters
 * @throws An error if the element is not found or component failed to create
 */
export async function addRootComponent<T extends HTMLElement>(targetElement: T, componentIdentifier: string, initialParameters: ComponentParameters): Promise<IDynamicRootComponent> {
    if (!targetElement){
        logAndThrow(`Element must be specified`);
    }
    try {
        const parameters = initialParameters === undefined || initialParameters === null ? {} : initialParameters;
        log.debug(`Initializing element ${targetElement} as ${componentIdentifier} using params: ${JSON.stringify(initialParameters)}`);
        return await BlazorFacade.rootComponents.add(targetElement, componentIdentifier, parameters);
    } catch (error) {
        logAndThrow(`Error adding Blazor root component on element '${targetElement}'`, error);
    }
}

/**
 * Logs an error message and throws an error.
 * @param errorMessage The error message to log and throw.
 * @param error (Optional) The error object to include in the message.
 * @throws An error with the specified error message.
 */
function logAndThrow(errorMessage: string, error?: Error): never {
    if (error) {
        errorMessage += `: ${error.message}`;
    }
    log.error(errorMessage);
    throw new Error(errorMessage);
}

function getElementByIdOrThrow<T extends HTMLElement>(elementId: string): T{
    const targetElement = document.getElementById(elementId) as T | null;

    if (!targetElement) {
        logAndThrow(`Element with ID '${elementId}' not found`);
    }

    return targetElement;
}