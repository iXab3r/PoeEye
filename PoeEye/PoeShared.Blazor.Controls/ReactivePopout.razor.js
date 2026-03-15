let lastMouseX = null;
let lastMouseY = null;
let isTrackingInstalled = false;

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

export function ensureReactivePopoutTracking() {
    if (isTrackingInstalled) {
        return;
    }

    isTrackingInstalled = true;
    document.addEventListener("mousemove", event => {
        lastMouseX = event.clientX;
        lastMouseY = event.clientY;
    }, true);
}

export function positionReactivePopout(panel) {
    if (!panel) {
        return;
    }

    panel.style.visibility = "hidden";
    panel.style.opacity = "0";

    const padding = 8;
    const gap = 6;
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    panel.style.maxWidth = `${Math.max(240, viewportWidth - (padding * 2))}px`;
    panel.style.maxHeight = `${Math.max(240, viewportHeight - (padding * 2))}px`;

    const panelWidth = Math.min(panel.offsetWidth || 352, viewportWidth - (padding * 2));
    const panelHeight = Math.min(panel.offsetHeight || 600, viewportHeight - (padding * 2));

    const anchorX = lastMouseX ?? Math.round(viewportWidth / 2);
    const anchorY = lastMouseY ?? Math.round(viewportHeight / 2);

    let left = anchorX + gap;
    let top = anchorY + gap;

    if (left + panelWidth > viewportWidth - padding) {
        left = anchorX - panelWidth - gap;
    }

    if (top + panelHeight > viewportHeight - padding) {
        top = anchorY - panelHeight - gap;
    }

    left = clamp(left, padding, Math.max(padding, viewportWidth - panelWidth - padding));
    top = clamp(top, padding, Math.max(padding, viewportHeight - panelHeight - padding));

    panel.style.left = `${Math.round(left)}px`;
    panel.style.top = `${Math.round(top)}px`;
    panel.style.visibility = "visible";
    panel.style.opacity = "1";
    panel.focus();
}
