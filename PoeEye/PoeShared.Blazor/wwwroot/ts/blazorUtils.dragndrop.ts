import log from 'loglevel';
import {CoreWebView2, getChromeWebView2} from './blazorUtils.common'

log.setLevel('info'); // Change this level as needed.

/**
 * Registers an HTML element as a file drop target for WebView2.
 *
 * This function attaches `dragover` and `drop` event listeners to the given element,
 * enabling users to drag files from the operating system and drop them onto the element.
 * When files are dropped, the function posts a structured WebView2 message using
 * `postMessageWithAdditionalObjects`, sending the dropped `FileList` to the .NET side.
 *
 * The .NET side should handle the message with:
 * - `WebMessageAsJson === "FilesDropped"`
 * - `AdditionalObjects` containing one or more `CoreWebView2File` instances.
 *
 * Requirements:
 * - This must be called after WebView2 is initialized.
 * - `window.chrome.webview.postMessageWithAdditionalObjects` must be available (WebView2 SDK >= 1.0.1518.46).
 *
 * @param element - The DOM element that will act as a drop target for files.
 *
 * @example
 * registerFileDropTarget(document.getElementById("drop-zone"));
 *
 * // In .NET:
 * webView.CoreWebView2.WebMessageReceived += (s, e) =>
 * {
 *     if (e.WebMessageAsJson == "\"FilesDropped\"")
 *     {
 *         foreach (var file in e.AdditionalObjects.OfType<CoreWebView2File>())
 *         {
 *             Console.WriteLine($"Dropped: {file.Name}, {file.Size} bytes");
 *         }
 *     }
 * };
 */
export function registerFileDropTarget(element: HTMLElement)  {
    const webview = getChromeWebView2();
    if (!webview.postMessageWithAdditionalObjects) {
        log.warn("WebView2 postMessageWithAdditionalObjects is not available.");
        return;
    }

    const onDragOver = (e: DragEvent) => {
        e.preventDefault();
        e.dataTransfer!.dropEffect = "copy";
    };

    const onDrop = (e: DragEvent) => {
        e.preventDefault();

        const files = e.dataTransfer?.files;
        if (files && files.length > 0) {
            try {
                // Note: FileList is already ArrayLike
                webview.postMessageWithAdditionalObjects("FilesDropped", files);
            } catch (err) {
                log.error("Failed to post dropped files:", err);
            }
        }
    };

    element.addEventListener("dragover", onDragOver);
    element.addEventListener("drop", onDrop);
}

