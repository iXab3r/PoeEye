const enum Direction {
    Horizontal,
    Vertical
}

const enum UnitOfSize {
    Pixel,
    Percent
}

const enum Props {
    Dir,
    TargetPaneIndex,
    PivotPos,
    InitSize,
    Unit,
    Disposed,
}

type TargetPaneIndex = 0 | 1;
type Position = number & { readonly brand: unique symbol };
type Size = number & { readonly brand: unique symbol };

type State = {
    [Props.Dir]: Direction;
    [Props.TargetPaneIndex]: TargetPaneIndex;
    [Props.PivotPos]: Position;
    [Props.InitSize]: Size;
    [Props.Unit]: UnitOfSize;
    [Props.Disposed]: boolean;
};

const EVENTS = {
    POINTER_DOWN: "pointerdown",
    POINTER_MOVE: "pointermove",
    POINTER_UP: "pointerup",
    TOUCH_START: "touchstart"
};

const NULL_ELEMENT = null;

interface DotNetObjectRef {
    invokeMethodAsync(methodName: string, ...args: any[]): Promise<any>
}

export const attach = (component: DotNetObjectRef, container: HTMLElement) => {
    if (!container) {
        throw new Error("Container element is required and cannot be null.");
    }

    const splitter = container.querySelector(":scope > .splitter-bar") as HTMLElement;
    if (!splitter) {
        throw new Error("Splitter bar element ('.spliter-bar') is missing in the container.");
    }

    const panes = Array.from(container.querySelectorAll(":scope > .pane-of-split-container")) as [HTMLElement, HTMLElement];
    if (panes.length !== 2) {
        throw new Error("The split container must contain exactly two pane elements with class '.pane-of-split-container'.");
    }

    const state: State = {
        [Props.Dir]: Direction.Horizontal,
        [Props.TargetPaneIndex]: 0,
        [Props.PivotPos]: 0 as Position,
        [Props.InitSize]: 0 as Size,
        [Props.Unit]: UnitOfSize.Pixel,
        [Props.Disposed]: false
    };

    const roundPos = (value: number) => Math.round(value) as Position;
    const roundSize = (value: number) => Math.round(value) as Size;

    const getPointerPosition = (ev: PointerEvent): Position =>
        roundPos(state[Props.Dir] === Direction.Horizontal ? ev.clientX : ev.clientY);

    const getElementSize = (el: HTMLElement): Size =>
        roundSize(state[Props.Dir] === Direction.Horizontal ? el.getBoundingClientRect().width : el.getBoundingClientRect().height);

    const getPaneSize = (paneIndex: number): Size => getElementSize(panes[paneIndex]);

    const addEvent = splitter.addEventListener.bind(splitter);
    const removeEvent = splitter.removeEventListener.bind(splitter);

    const dispose = () => {
        if (state[Props.Disposed]) return;
        state[Props.Disposed] = true;
        removeEvent(EVENTS.POINTER_DOWN, onPointerDown);
        removeEvent(EVENTS.POINTER_UP, onPointerUp);
        removeEvent(EVENTS.TOUCH_START, preventDefault);
    };

    const preventDefault = (ev: TouchEvent) => ev.preventDefault();

    const updatePaneSize = (ev: PointerEvent): [HTMLElement | null, number] => {
        const targetPane = panes[state[Props.TargetPaneIndex]] ?? NULL_ELEMENT;
        if (!targetPane) return [NULL_ELEMENT, 0];

        const delta = getPointerPosition(ev) - state[Props.PivotPos];
        const newSizePx = state[Props.InitSize] + (state[Props.TargetPaneIndex] === 0 ? delta : -delta);
        const newSizeUnit = state[Props.Unit] === UnitOfSize.Percent
            ? parseFloat(((newSizePx + getElementSize(splitter) / 2) / getElementSize(container) * 100).toFixed(3))
            : newSizePx;

        const newSize = state[Props.Unit] === UnitOfSize.Percent
            ? `calc(${newSizeUnit}% - calc(var(--splitter-bar-size) / 2))`
            : `${newSizeUnit}px`;

        if (state[Props.Dir] === Direction.Horizontal) {
            targetPane.style.width = newSize;
        } else {
            targetPane.style.height = newSize;
        }

        return [targetPane, newSizeUnit];
    };

    const onPointerMove = (ev: PointerEvent) => updatePaneSize(ev);

    const onPointerDown = (ev: PointerEvent) => {
        if (!document.contains(splitter)) {
            dispose();
            return;
        }

        const targetPaneIndex = panes.findIndex(pane => pane.style.flex === "") as TargetPaneIndex | -1;
        if (targetPaneIndex === -1) return;

        state[Props.Dir] = container.classList.contains("splitter-orientation-vertical") ? Direction.Horizontal : Direction.Vertical;
        state[Props.TargetPaneIndex] = targetPaneIndex;
        state[Props.PivotPos] = getPointerPosition(ev);
        state[Props.InitSize] = getPaneSize(targetPaneIndex);
        state[Props.Unit] = container.dataset.unitOfSize === "percent" ? UnitOfSize.Percent : UnitOfSize.Pixel;

        addEvent(EVENTS.POINTER_MOVE, onPointerMove);
        splitter.setPointerCapture(ev.pointerId);
    };

    const onPointerUp = (ev: PointerEvent) => {
        splitter.releasePointerCapture(ev.pointerId);
        removeEvent(EVENTS.POINTER_MOVE, onPointerMove);
        const [resizedPane, newSizeUnit] = updatePaneSize(ev);
        component.invokeMethodAsync("UpdateSize", resizedPane === panes[0], newSizeUnit);
    };

    addEvent(EVENTS.POINTER_DOWN, onPointerDown);
    addEvent(EVENTS.POINTER_UP, onPointerUp);
    addEvent(EVENTS.TOUCH_START, preventDefault, { passive: false });

    return { dispose };
};
