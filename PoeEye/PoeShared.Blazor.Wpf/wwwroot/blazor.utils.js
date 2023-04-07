console.info(`Blazor Helpers are loaded`)

function loadJs(sourceUrl) {
    if (!sourceUrl || sourceUrl.Length === 0) {
        console.error("Invalid source URL");
        return;
    }

    const tag = document.createElement('script');
    tag.src = sourceUrl;
    tag.type = "text/javascript";

    tag.onload = function () {
        console.log("Script loaded successfully");
    }

    tag.onerror = function () {
        console.error(`Failed to load script ${sourceUrl}`);
    }

    document.body.appendChild(tag);
}

function loadCss(sourceUrl) {
    if (!sourceUrl || sourceUrl.Length === 0) {
        console.error("Invalid source URL");
        return;
    }

    const tag = document.createElement('link');
    tag.href = sourceUrl;
    tag.rel = "stylesheet";

    tag.onload = function () {
        console.log(`CSS loaded successfully ${sourceUrl}`);
    }

    tag.onerror = function () {
        console.error(`Failed to load CSS ${sourceUrl}`);
    }

    document.head.appendChild(tag);
}