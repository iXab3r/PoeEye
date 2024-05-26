import log from 'loglevel';

type ScriptLoaderStatus = 'loading' | 'loaded' | 'failed';
const loadedScriptsByPath: Map<string, ScriptLoaderStatus> = new Map();
const loadedCssByPath: Map<string, ScriptLoaderStatus> = new Map();

/**
 * loadScript - Loads a JavaScript file dynamically and returns a promise that completes when the script loads.
 * @param scriptPath - The path to the JavaScript file to load.
 * @returns A promise that resolves with the script path when the script is loaded successfully.
 */
export function loadScript(scriptPath: string): Promise<void> {
    const existingStatus = loadedScriptsByPath.get(scriptPath);
    if (existingStatus) {
        log.info(`${scriptPath} already present in cache with status ${existingStatus}`);
        return Promise.resolve();
    }

    return new Promise<void>((resolve, reject) => {
        const script: HTMLScriptElement = document.createElement("script");
        script.src = scriptPath;
        script.type = "text/javascript";
        
        log.info(`${scriptPath} started loading`);
        loadedScriptsByPath.set(scriptPath, 'loading');

        script.onload = () => {
            log.info(`${scriptPath} loaded`);
            loadedScriptsByPath.set(scriptPath, 'loaded');
            resolve();
        };
        
        script.onerror = () => {
            log.info(`Failed to load ${scriptPath}`);
            loadedScriptsByPath.set(scriptPath, 'failed');
            reject(new Error(`${scriptPath} failed to load`));
        };

        document.body.appendChild(script);
    });
}

export function loadCss(cssPath: string): Promise<void> {
    const existingStatus = loadedCssByPath.get(cssPath);
    if (existingStatus) {
        log.info(`${cssPath} already present in cache with status ${existingStatus}`);
        return Promise.resolve();
    }

    if (!cssPath) {
        console.error("Invalid CSS URL");
        return;
    }

    return new Promise<void>((resolve, reject) => {
        log.info(`CSS ${cssPath} started loading`);
        loadedCssByPath.set(cssPath, 'loading');

        const tag = document.createElement('link');
        tag.href = cssPath;
        tag.rel = "stylesheet";

        tag.onload = () => {
            log.info(`CSS ${cssPath} loaded`);
            loadedCssByPath.set(cssPath, 'loaded');
            resolve();
        };

        tag.onerror = () => {
            log.info(`Failed to load CSS ${cssPath}`);
            loadedCssByPath.set(cssPath, 'failed');
            reject(new Error(`CSS ${cssPath} failed to load`));
        };

        document.head.appendChild(tag);
    });
}
