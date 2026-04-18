window.poeAvaloniaWindowResize = (() => {
    let activeCleanup = null;

    function toPhysical(value) {
        const dpr = window.devicePixelRatio || 1;
        return Math.round(value * dpr);
    }

    function disposeActive() {
        if (activeCleanup) {
            activeCleanup();
            activeCleanup = null;
        }
    }

    function beginBottomRightResize(dotNetRef, startClientX, startClientY) {
        disposeActive();

        dotNetRef.invokeMethodAsync("BeginResizeSession", toPhysical(startClientX), toPhysical(startClientY));

        const onMove = moveEvent => {
            dotNetRef.invokeMethodAsync("UpdateResizeSession", toPhysical(moveEvent.clientX), toPhysical(moveEvent.clientY));
        };

        const onUp = upEvent => {
            dotNetRef.invokeMethodAsync("UpdateResizeSession", toPhysical(upEvent.clientX), toPhysical(upEvent.clientY));
            dotNetRef.invokeMethodAsync("EndResizeSession");
            disposeActive();
        };

        const onBlur = () => {
            dotNetRef.invokeMethodAsync("EndResizeSession");
            disposeActive();
        };

        window.addEventListener("mousemove", onMove, true);
        window.addEventListener("mouseup", onUp, true);
        window.addEventListener("blur", onBlur, true);

        activeCleanup = () => {
            window.removeEventListener("mousemove", onMove, true);
            window.removeEventListener("mouseup", onUp, true);
            window.removeEventListener("blur", onBlur, true);
        };
    }

    return {
        beginBottomRightResize
    };
})();
