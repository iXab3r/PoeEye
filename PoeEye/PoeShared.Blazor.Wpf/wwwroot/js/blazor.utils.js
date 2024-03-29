﻿console.info(`Blazor Helpers are loaded`)

function loadJs(sourceUrl) {
    if (!sourceUrl || sourceUrl.Length === 0) {
        console.error("Invalid source URL");
        return;
    }

    const tag = document.createElement('script');
    tag.src = sourceUrl;
    tag.type = "text/javascript";

    tag.onload = function () {
        console.log(`Script loaded successfully: ${sourceUrl}`);
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

function scrollToBottom() {
    document.body.scroll(0, document.body.scrollHeight)
}

function scrollToTop() {
    document.body.scroll(0, 0)
}

function scrollToSelectedOption(selectElementId) {
    const $selectElement = $('#' + selectElementId);
    const $selectedOption = $selectElement.find('option:selected');

    if ($selectedOption.length) {
        $selectedOption[0].scrollIntoView({behavior: 'smooth', block: 'nearest'});
    }
}

function throwException(message) {
    console.warn(`Throwing exception: ${message}`)
    throw new Error(message);
}