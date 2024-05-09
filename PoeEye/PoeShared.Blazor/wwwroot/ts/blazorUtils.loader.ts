import log from 'loglevel';

type ScriptLoaderStatus = 'loading' | 'loaded' | 'failed';
const loadedScriptsByPath: Map<string, ScriptLoaderStatus> = new Map();

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
