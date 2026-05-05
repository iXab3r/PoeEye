// noinspection JSUnusedGlobalSymbols

import {
    ComponentItem,
    ComponentItemConfig,
    ContentItem,
    GoldenLayout,
    Header,
    ItemType,
    LayoutConfig,
    LayoutManager,
    Stack
} from 'golden-layout';
import {addRootComponent, ComponentParameters} from "./BlazorFacade";
import {DotNetObjectReference} from "./dot-net-object-reference";

import log from 'loglevel';

export function create(target: HTMLElement): GoldenLayoutInterop {
    log.info(`Initializing new GoldenLayout, element: ${target}`)
    return new GoldenLayoutInterop(target);
}

interface IRfComponentState {
    query: string;
    title?: string;
    id?: string;
    closeable?: boolean;
    reorderEnabled?: boolean;
    headerShow?: string;
    size?: string;
    minSize?: string;
}

interface IBlazorComponentState {
    dynamicComponentId: string;
    tabDynamicComponentId?: string;
    title?: string;
    id?: string;
    closeable?: boolean;
    reorderEnabled?: boolean;
    headerShow?: string;
    size?: string;
    minSize?: string;
}

interface IGoldenLayoutLocationInfo {
    id?: string;
    itemType?: string;
    index?: number;
}

type GoldenLayoutHeaderDragStartPrototype = Header & {
    _componentDragStartEvent?: (x: number, y: number, dragListener: unknown, componentItem: ComponentItem) => void;
    handleTabInitiatedDragStartEvent?: (x: number, y: number, dragListener: unknown, componentItem: ComponentItem) => void;
    __eyeAurasTabRedragPatchApplied?: boolean;
};

// GoldenLayout 2.6 ties tab drag start to closability; keep close/remove guarded, but let reorder start.
function patchGoldenLayoutTabRedrag() {
    const headerPrototype = Header.prototype as GoldenLayoutHeaderDragStartPrototype;
    if (headerPrototype.__eyeAurasTabRedragPatchApplied) {
        return;
    }

    const originalDragStart = headerPrototype.handleTabInitiatedDragStartEvent;
    if (typeof originalDragStart !== 'function') {
        log.debug('GoldenLayout Header tab drag-start hook not found; tab redrag patch was not applied');
        return;
    }

    headerPrototype.handleTabInitiatedDragStartEvent = function (x, y, dragListener, componentItem) {
        if (this._componentDragStartEvent === undefined) {
            originalDragStart.call(this, x, y, dragListener, componentItem);
            return;
        }

        this._componentDragStartEvent(x, y, dragListener, componentItem);
    };
    headerPrototype.__eyeAurasTabRedragPatchApplied = true;
}

class GoldenLayoutInterop {

    private readonly rfComponentType: string = 'gl-rf';
    private readonly blazorComponentType: string = 'gl-blazor-dynamic-component';
    private readonly dynamicComponentName: string = 'blazor-dynamic-component';
    private readonly target: HTMLElement;
    private readonly goldenLayout?: GoldenLayout;
    private hookRefs: Map<number, DotNetObjectReference> = new Map<number, DotNetObjectReference>();
    private lastNotifiedFocusComponentId?: string;
    private pendingFocusComponentId?: string;
    private isDispatchingFocus = false;
    /**
     * GoldenLayout 2.6 tracks splitter drag on the document, but does not capture the pointer.
     * In our WebView2 host this shows up as two UX bugs:
     * 1. text selection outside the splitter can start while resize is starting
     * 2. pointerup can occasionally be missed, leaving resize "stuck" until the next click
     */
    private activeSplitterPointerId?: number;
    private activeSplitterElement?: HTMLElement;


    constructor(target: HTMLElement) {
        this.target = target;
        patchGoldenLayoutTabRedrag();
        this.installSplitterPointerCapture();
        this.goldenLayout = new GoldenLayout(target);
        this.goldenLayout.resizeWithContainerAutomatically = true;

        this.getGoldenLayout().addEventListener('focus', (e) => {
            const componentItem = e.target as ComponentItem;
            this.handleFocus(componentItem).catch(error => log.debug('Failed to handle GoldenLayout focus change', error));
        });

        this.goldenLayout.registerComponentFactoryFunction(this.rfComponentType, (container) => {
            const state = container.state as IRfComponentState;
            log.info(`Configuring new component, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);
            if (state.id) {
                container.parent.id = state.id;
            }

            const element = document.querySelector(state.query);
            container.element.append(element)
        });

        this.goldenLayout.registerComponentFactoryFunction(this.blazorComponentType, (container) => {
            const state = container.state as IBlazorComponentState;
            log.debug(`Configuring new Blazor component, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);

            const blazorComponentParameters: ComponentParameters = [];
            const dynamicComponentName = this.dynamicComponentName;
            blazorComponentParameters["Id"] = state.dynamicComponentId;
            addRootComponent(container.element, dynamicComponentName, blazorComponentParameters).then(r => {
                log.debug(`Configured new Blazor component, root: ${r}, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);

                container.on('destroy', () => {
                    log.debug(`GL container was destroyed, root: ${r}, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);
                    r.dispose().then(r => {
                        log.debug(`Dynamic component was disposed, root: ${r}, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);
                    });
                })
            });

            container.on('tab', function (tab) {
                if (state.tabDynamicComponentId) {
                    tab.element.classList.add("hover-container");
                    tab.setTitle('');

                    const parameters: ComponentParameters = {
                        Id: state.tabDynamicComponentId
                    };

                    addRootComponent(tab.titleElement, dynamicComponentName, parameters).then(r => {
                        log.debug(`Configured new Blazor Tab component, root: ${r}, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);

                        container.on('destroy', () => {
                            log.debug(`GL Tab container was destroyed, root: ${r}, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);
                            r.dispose().then(r => {
                                log.debug(`Dynamic Tab component was disposed, root: ${r}, parent: ${container.parent} (parent.id: ${container.parent?.id}, ${container.width}x${container.height}, title: ${container.title}): ${JSON.stringify(container.state)}`);
                            });
                        })
                    });
                }
            });

            container.element.addEventListener('click', () => {
                container.focus();
            });

            container.element.addEventListener('focusin', () => {
                container.focus();
            });

        });
    }

    public dispose() {
        this.releaseTrackedSplitterPointerCapture();
        this.target.removeEventListener('pointerdown', this.handleSplitterPointerDown, true);
        document.removeEventListener('pointerup', this.handleTrackedSplitterPointerEnd, true);
        document.removeEventListener('pointercancel', this.handleTrackedSplitterPointerEnd, true);
        document.removeEventListener('lostpointercapture', this.handleTrackedSplitterPointerEnd, true);
        this.goldenLayout?.destroy();
    }

    public focusById(id: string) {
        const componentItem = this.goldenLayout.findFirstComponentItemById(id);
        if (!componentItem) {
            throw `Could not find parent component by Id ${id}`;
        }
        this.lastNotifiedFocusComponentId = id;
        componentItem.focus(true);
    }

    public addHook(hookRef: DotNetObjectReference) {
        log.info(`Adding .net hook ${hookRef} (total: ${this.hookRefs.size})`);
        this.hookRefs.set(hookRef._id, hookRef);
        log.info(`Added .net hook ${hookRef} (total: ${this.hookRefs.size})`);
    }

    public removeHook(hookRef: DotNetObjectReference) {
        log.info(`Removing .net hook ${hookRef} (total: ${this.hookRefs.size})`);

        if (this.hookRefs.has(hookRef._id)) {
            this.hookRefs.delete(hookRef._id);
            log.info(`Removed .net hook ${hookRef} (total: ${this.hookRefs.size})`);
        } else {
            throw `Failed to find specified .net hook in hook list`
        }
    }

    public loadLayout(config: LayoutConfig) {
        log.info(`Loading GoldenLayout layout, config: ${JSON.stringify(config)}`)
        this.getGoldenLayout().loadLayout(config);
    }

    public getGoldenLayout(): GoldenLayout {
        return this.goldenLayout;
    }

    public removeItemById(id: string) {
        const componentItem = this.findFirstContentItemById(id, ItemType.component); 
        if (!componentItem) {
            throw `Could not find component by Id ${id}`;
        }
        componentItem.remove()
    }

    public addChildItem(parentId: string, state: IRfComponentState): IGoldenLayoutLocationInfo {
        log.info(`Adding new GoldenLayout component, parentId: ${parentId}, state: ${JSON.stringify(state)}`)
        this.ensureReady();

        const newItemConfig = this.createConfig(state);
        const index = this.addItemToStack(parentId, newItemConfig);

        log.info(`Added new child component Id ${newItemConfig.id} to component Id ${parentId}`);
        return {
            index: index,
            id: newItemConfig.id,
        };
    }

    /**
     * Adds a new ComponentItem.  Will use default location selectors to ensure a location is found and
     * component is successfully added
     * @param state - Optional initial state to be assigned to component
     * @returns Location of new ComponentItem created.
     */
    public addItem(state: IRfComponentState): IGoldenLayoutLocationInfo {
        log.info(`Adding new GoldenLayout component, state: ${JSON.stringify(state)}`)
        this.ensureReady();

        const newItemConfig = this.createConfig(state);
        const location = this.goldenLayout.addItem(newItemConfig);
        const locationInfo = this.toLocationInfo(location);
        log.info(`Added new component: ${JSON.stringify(locationInfo)}`);
        return locationInfo;
    }

    public addBlazorChildItem(parentId: string, state: IBlazorComponentState): IGoldenLayoutLocationInfo {
        log.info(`Adding new GoldenLayout Blazor child component, parentId: ${parentId}, state: ${JSON.stringify(state)}`)
        const newItemConfig = this.createBlazorConfig(state);
        const index = this.addItemToStack(parentId, newItemConfig);
        log.info(`Added new Blazor child component Id ${newItemConfig.id} to parent of component Id ${parentId}`);
        return {
            index: index,
            id: newItemConfig.id,
        };
    }

    public addBlazorItem(state: IBlazorComponentState): IGoldenLayoutLocationInfo {
        log.info(`Adding new GoldenLayout Blazor component, state: ${JSON.stringify(state)}`)
        this.ensureReady();

        const newItemConfig = this.createBlazorConfig(state);
        const location = this.goldenLayout.addItem(newItemConfig);
        const locationInfo = this.toLocationInfo(location);
        log.info(`Added new Blazor component: ${JSON.stringify(locationInfo)}`);
        return locationInfo;
    }

    public addBlazorItemAtLocation(state: IBlazorComponentState, locationSelectors?: readonly LayoutManager.LocationSelector[]): IGoldenLayoutLocationInfo {
        log.info(`Adding new GoldenLayout Blazor component at location, state: ${JSON.stringify(state)}`)
        this.ensureReady();

        const newItemConfig = this.createBlazorConfig(state);
        const location = this.goldenLayout.addItemAtLocation(newItemConfig, locationSelectors);
        const locationInfo = this.toLocationInfo(location);
        log.info(`Added new Blazor component at location: ${JSON.stringify(locationInfo)}`);
        return locationInfo;
    }

    /**
     * Adds a ComponentItem at the first valid selector location.
     * @param state - Optional initial state to be assigned to component
     * @param locationSelectors - Array of location selectors used to find determine location in layout where component
     * will be added. First location in array which is valid will be used. If undefined,
     * {@link (LayoutManager:namespace).defaultLocationSelectors} will be used.
     * @returns Location of new ComponentItem created or undefined if no valid location selector was in array.
     */
    public addItemAtLocation(state: IRfComponentState, locationSelectors?: readonly LayoutManager.LocationSelector[]): IGoldenLayoutLocationInfo {
        log.info(`Adding new GoldenLayout component at location, state: ${JSON.stringify(state)}, selectors: ${JSON.stringify(locationSelectors)}`);
        this.ensureReady();

        const newItemConfig = this.createConfig(state);

        const location = this.goldenLayout.addItemAtLocation(newItemConfig, locationSelectors);
        const locationInfo = this.toLocationInfo(location);
        log.info(`Added new component at location: ${JSON.stringify(locationInfo)}`);
        return locationInfo;
    }

    private addItemToStack(parentId: string, newItemConfig: ComponentItemConfig): number {
        const contentItem = this.findFirstContentItemById(parentId);
        if (!contentItem) {
            throw `Could not find parent component by Id ${parentId}`;
        }
        if (!contentItem.isStack) {
            throw `Component with Id ${contentItem.id} must be Stack, currently: ${contentItem.type}`;
        }
        const stack: Stack = contentItem as Stack;
        return stack.addItem(newItemConfig);
    }

    private async handleFocus(focused: ComponentItem | undefined) {
        const focusedId = focused?.id;
        if (!focusedId) {
            log.debug('Ignoring GoldenLayout focus change without component id', focused);
            return;
        }

        this.pendingFocusComponentId = focusedId;
        if (this.isDispatchingFocus) {
            return;
        }

        this.isDispatchingFocus = true;
        try {
            while (this.pendingFocusComponentId !== undefined) {
                const componentId = this.pendingFocusComponentId;
                this.pendingFocusComponentId = undefined;

                if (componentId === this.lastNotifiedFocusComponentId) {
                    continue;
                }

                this.lastNotifiedFocusComponentId = componentId;
                log.debug(`Focus changed: ${componentId}`);

                for (const hookRef of Array.from(this.hookRefs.values())) {
                    try {
                        await hookRef.invokeMethodAsync("HandleFocusChanged", componentId);
                    } catch (error) {
                        log.debug(`Failed to notify .NET hook about focus change to ${componentId}`, error);
                    }
                }
            }
        } finally {
            this.isDispatchingFocus = false;
        }
    }

    /**
     * This wrapper-level shim is the smallest low-risk fix we can keep locally:
     * - apply pointer capture only to splitters
     * - suppress text selection from the initial pointerdown
     * - release on pointerup, pointercancel, and lostpointercapture
     *
     * That keeps the resize interaction exclusive without forking GoldenLayout or changing
     * unrelated drag paths such as tab reordering.
     */
    private installSplitterPointerCapture() {
        this.target.addEventListener('pointerdown', this.handleSplitterPointerDown, true);
        document.addEventListener('pointerup', this.handleTrackedSplitterPointerEnd, true);
        document.addEventListener('pointercancel', this.handleTrackedSplitterPointerEnd, true);
        document.addEventListener('lostpointercapture', this.handleTrackedSplitterPointerEnd, true);
    }

    private readonly handleSplitterPointerDown = (event: PointerEvent) => {
        if (event.button !== 0 || !event.isPrimary) {
            return;
        }

        const splitter = this.tryGetSplitterElement(event.target);
        if (!splitter) {
            return;
        }

        this.releaseTrackedSplitterPointerCapture();

        this.activeSplitterPointerId = event.pointerId;
        this.activeSplitterElement = splitter;
        document.body.classList.add('ea-gl-splitter-pending');

        try {
            splitter.setPointerCapture(event.pointerId);
        } catch (error) {
            log.debug('Failed to capture splitter pointer', error);
        }

        event.preventDefault();
    };

    private readonly handleTrackedSplitterPointerEnd = (event: PointerEvent) => {
        if (this.activeSplitterPointerId !== event.pointerId) {
            return;
        }

        this.releaseTrackedSplitterPointerCapture();
    };

    private releaseTrackedSplitterPointerCapture() {
        document.body.classList.remove('ea-gl-splitter-pending');

        const splitter = this.activeSplitterElement;
        const pointerId = this.activeSplitterPointerId;

        this.activeSplitterElement = undefined;
        this.activeSplitterPointerId = undefined;

        if (!splitter || pointerId === undefined) {
            return;
        }

        try {
            if (splitter.hasPointerCapture(pointerId)) {
                splitter.releasePointerCapture(pointerId);
            }
        } catch (error) {
            log.debug('Failed to release splitter pointer capture', error);
        }
    }

    private tryGetSplitterElement(target: EventTarget | null): HTMLElement | undefined {
        if (!(target instanceof Element)) {
            return undefined;
        }

        const splitter = target.closest('.lm_splitter');
        if (!(splitter instanceof HTMLElement)) {
            return undefined;
        }

        if (!this.target.contains(splitter)) {
            return undefined;
        }

        return splitter;
    }

    private ensureReady(): GoldenLayout {
        if (!this.goldenLayout.rootItem) {
            //throw "GoldenLayout rootItem not available — maybe not initialized yet."
        }
        return this.goldenLayout;
    }

    private createConfig(state: IRfComponentState): ComponentItemConfig {
        return {
            type: 'component',
            componentType: this.rfComponentType,
            componentState: state,
            title: state.title,
            id: state.id,
            isClosable: state.closeable,
            reorderEnabled: state.reorderEnabled,
            size: state.size,
            minSize: state.minSize,
            header: {
                show: "bottom",

            },
        };
    }

    private createBlazorConfig(state: IBlazorComponentState): ComponentItemConfig {
        return {
            type: 'component',
            componentType: this.blazorComponentType,
            componentState: state,
            title: state.title,
            id: state.id,
            isClosable: state.closeable,
            reorderEnabled: state.reorderEnabled,
            size: state.size,
            minSize: state.minSize,
            header: {
                show: "bottom",

            },
        };
    }

    private toLocationInfo(location: LayoutManager.Location): IGoldenLayoutLocationInfo {
        return {
            index: location.index,
            itemType: location.parentItem?.type,
            id: location.parentItem?.id
        };
    }

    /**
     * Finds the first ContentItem by ID, optionally filtered by item type.
     * If `type` is omitted, any type will match.
     *
     * Adapted from:
     * https://github.com/golden-layout/golden-layout/blob/95af36d1e4c596696483c5353d32ae71886b999c/src/ts/layout-manager.ts#L749
     *
     * @param id - The ID of the content item to find.
     * @param type - Optional type of item to match (e.g., 'component', 'row', etc.).
     * @returns The first matching ContentItem or undefined.
     */
    private findFirstContentItemById(id: string, type?: ItemType): ContentItem | undefined {
        const layout = this.ensureReady();
        return this.findFirstContentItemByIdRecursive(id, layout.rootItem, type);
    }

    /**
     * Recursively searches the layout tree starting from the given node,
     * matching the specified ID and (optionally) ItemType.
     *
     * @param id - The target ID to look for.
     * @param node - The current subtree root.
     * @param type - Optional filter for ItemType.
     */
    private findFirstContentItemByIdRecursive(
        id: string,
        node: ContentItem,
        type?: ItemType
    ): ContentItem | undefined {
        const contentItems = node.contentItems;

        for (const item of contentItems) {
            // Match if ID is correct and type matches (or no type is specified)
            const matchesType = type === undefined || item.type === type;
            if (item.id === id && matchesType) {
                return item;
            }
        }

        // Recursively search deeper into the layout tree
        for (const item of contentItems) {
            const found = this.findFirstContentItemByIdRecursive(id, item, type);
            if (found !== undefined) {
                return found;
            }
        }

        return undefined;
    }
}
