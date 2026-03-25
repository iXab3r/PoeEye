let lastMouseX = null;
let lastMouseY = null;
let isTrackingInstalled = false;
const panelStates = new WeakMap();

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function rememberPointer(event) {
    lastMouseX = event.clientX;
    lastMouseY = event.clientY;
}

function isPopoverOpen(panel) {
    return Boolean(panel?.matches(":popover-open"));
}

function showPopover(panel) {
    if (panel && !isPopoverOpen(panel)) {
        panel.showPopover();
    }
}

function hidePopover(panel) {
    if (panel && isPopoverOpen(panel)) {
        panel.hidePopover();
    }
}

function cleanupReactivePopout(panel) {
    const state = panelStates.get(panel);
    if (!state) {
        return;
    }

    state.cleanup?.();
    panelStates.delete(panel);
}

function getViewportAnchor() {
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    return {
        x: lastMouseX ?? Math.round(viewportWidth / 2),
        y: lastMouseY ?? Math.round(viewportHeight / 2),
    };
}

function placeAnchor(anchor) {
    if (!anchor) {
        return;
    }

    const { x, y } = getViewportAnchor();
    anchor.style.left = `${Math.round(x)}px`;
    anchor.style.top = `${Math.round(y)}px`;
}

function getOverflow(left, top, panelWidth, panelHeight, viewportWidth, viewportHeight, padding) {
    const overflowLeft = Math.max(0, padding - left);
    const overflowTop = Math.max(0, padding - top);
    const overflowRight = Math.max(0, left + panelWidth - (viewportWidth - padding));
    const overflowBottom = Math.max(0, top + panelHeight - (viewportHeight - padding));

    return overflowLeft + overflowTop + overflowRight + overflowBottom;
}

function choosePlacement(anchorRect, panelWidth, panelHeight, viewportWidth, viewportHeight, padding, gap) {
    const placements = [
        {
            left: anchorRect.right + gap,
            top: anchorRect.bottom + gap,
        },
        {
            left: anchorRect.left - panelWidth - gap,
            top: anchorRect.bottom + gap,
        },
        {
            left: anchorRect.right + gap,
            top: anchorRect.top - panelHeight - gap,
        },
        {
            left: anchorRect.left - panelWidth - gap,
            top: anchorRect.top - panelHeight - gap,
        },
    ];

    let bestPlacement = placements[0];
    let bestOverflow = Number.POSITIVE_INFINITY;

    for (const placement of placements) {
        const overflow = getOverflow(
            placement.left,
            placement.top,
            panelWidth,
            panelHeight,
            viewportWidth,
            viewportHeight,
            padding);

        if (overflow < bestOverflow) {
            bestPlacement = placement;
            bestOverflow = overflow;
        }
    }

    return bestPlacement;
}

function positionReactivePopoutCore(panel, anchor) {
    if (!panel || !anchor) {
        return;
    }

    if (!isPopoverOpen(panel)) {
        showPopover(panel);
    }

    panel.style.visibility = "hidden";
    panel.style.opacity = "0";

    const padding = 8;
    const gap = 6;
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const anchorRect = anchor.getBoundingClientRect();
    const panelRect = panel.getBoundingClientRect();
    const panelWidth = Math.min(panelRect.width || panel.offsetWidth || 352, Math.max(0, viewportWidth - (padding * 2)));
    const panelHeight = Math.min(panelRect.height || panel.offsetHeight || 600, Math.max(0, viewportHeight - (padding * 2)));
    const placement = choosePlacement(anchorRect, panelWidth, panelHeight, viewportWidth, viewportHeight, padding, gap);

    const left = clamp(placement.left, padding, Math.max(padding, viewportWidth - panelWidth - padding));
    const top = clamp(placement.top, padding, Math.max(padding, viewportHeight - panelHeight - padding));

    panel.style.left = `${Math.round(left)}px`;
    panel.style.top = `${Math.round(top)}px`;
    panel.style.visibility = "visible";
    panel.style.opacity = "1";
}

function installReactivePopoutState(panel, anchor, dotNetRef, options) {
    cleanupReactivePopout(panel);

    const requestClose = () => {
        if (!dotNetRef) {
            return;
        }

        dotNetRef.invokeMethodAsync("HandleJsCloseRequest")
            .catch(() => cleanupReactivePopout(panel));
    };

    const reposition = () => {
        if (!panel.isConnected) {
            cleanupReactivePopout(panel);
            return;
        }

        if (!isPopoverOpen(panel)) {
            return;
        }

        positionReactivePopoutCore(panel, anchor);
    };

    const handlePointerDown = event => {
        if (!panel.isConnected) {
            cleanupReactivePopout(panel);
            return;
        }

        if (!options.closeOnBackdropPointerDown) {
            return;
        }

        if (panel.contains(event.target)) {
            return;
        }

        requestClose();
    };

    const handleKeyDown = event => {
        if (!panel.isConnected) {
            cleanupReactivePopout(panel);
            return;
        }

        if (!options.closeOnEscape || event.key !== "Escape") {
            return;
        }

        requestClose();
    };

    const resizeObserver = new ResizeObserver(() => {
        queueMicrotask(reposition);
    });

    resizeObserver.observe(panel);
    window.addEventListener("resize", reposition, true);
    window.addEventListener("scroll", reposition, true);
    document.addEventListener("pointerdown", handlePointerDown, true);
    document.addEventListener("keydown", handleKeyDown, true);

    panelStates.set(panel, {
        cleanup() {
            resizeObserver.disconnect();
            window.removeEventListener("resize", reposition, true);
            window.removeEventListener("scroll", reposition, true);
            document.removeEventListener("pointerdown", handlePointerDown, true);
            document.removeEventListener("keydown", handleKeyDown, true);
        },
    });
}

export function ensureReactivePopoutTracking() {
    if (isTrackingInstalled) {
        return;
    }

    isTrackingInstalled = true;
    document.addEventListener("pointerdown", rememberPointer, true);
    document.addEventListener("pointermove", rememberPointer, true);
}

export function syncReactivePopout(panel, anchor, isOpen, dotNetRef, options) {
    if (!panel || !anchor) {
        return;
    }

    if (!isOpen) {
        cleanupReactivePopout(panel);
        hidePopover(panel);
        panel.style.left = "";
        panel.style.top = "";
        panel.style.visibility = "";
        panel.style.opacity = "";
        return;
    }

    placeAnchor(anchor);
    installReactivePopoutState(panel, anchor, dotNetRef, options ?? {});
    positionReactivePopoutCore(panel, anchor);
    panel.focus();
    requestAnimationFrame(() => {
        if (panelStates.has(panel)) {
            positionReactivePopoutCore(panel, anchor);
        }
    });
}

export function repositionReactivePopout(panel, anchor) {
    if (!panel || !anchor || !isPopoverOpen(panel)) {
        return;
    }

    positionReactivePopoutCore(panel, anchor);
}

export function disposeReactivePopout(panel) {
    if (!panel) {
        return;
    }

    cleanupReactivePopout(panel);
    hidePopover(panel);
}
