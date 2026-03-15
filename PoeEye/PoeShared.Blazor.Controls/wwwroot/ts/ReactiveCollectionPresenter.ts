type RootComponentParameters = Record<string, unknown>;

interface IDynamicRootComponent {
    setParameters(parameters: RootComponentParameters): void;
    dispose(): Promise<void>;
}

interface IRootComponentsFunctions {
    add(toElement: Element, componentIdentifier: string, initialParameters: RootComponentParameters): Promise<IDynamicRootComponent>;
}

interface IBlazor {
    rootComponents: IRootComponentsFunctions;
}

interface ReactiveCollectionJsItem {
    key: string;
    registrationId: string;
    registrationVersion: number;
    tagName: string;
    className?: string | null;
    style?: string | null;
}

interface ReactiveCollectionJsOperation extends Partial<ReactiveCollectionJsItem> {
    kind: "add" | "update" | "move" | "remove" | "clear" | "reset";
    beforeKey?: string | null;
    items?: ReactiveCollectionJsItem[];
}

interface ReactiveCollectionJsFrame {
    operations: ReactiveCollectionJsOperation[];
}

interface ReactiveCollectionItemHandle {
    hostElement: HTMLElement;
    rootComponent: IDynamicRootComponent;
    tagName: string;
}

const BASE_ITEM_CLASS = "poe-reactive-collection-item";
const BlazorFacade: IBlazor | undefined = (window as any).Blazor;

if (!BlazorFacade) {
    throw new Error("Blazor root components API is not available on window.Blazor.");
}

class ReactiveCollectionSession {
    private readonly itemsByKey = new Map<string, ReactiveCollectionItemHandle>();
    private pendingWork: Promise<void> = Promise.resolve();

    constructor(
        private readonly container: HTMLElement,
        private readonly emptyStateElement: HTMLElement | null,
        private readonly componentIdentifier: string
    ) {
        this.updateEmptyState();
    }

    public applyFrames(frames: ReactiveCollectionJsFrame[]): Promise<void> {
        this.pendingWork = this.pendingWork.then(async () => {
            for (const frame of frames) {
                await this.applyFrame(frame);
            }
        });

        return this.pendingWork;
    }

    public async dispose(): Promise<void> {
        await this.pendingWork;
        await this.clearItems();
    }

    private async applyFrame(frame: ReactiveCollectionJsFrame): Promise<void> {
        for (const operation of frame.operations ?? []) {
            switch (operation.kind) {
                case "add":
                    await this.addItem(operation);
                    break;
                case "update":
                    this.updateItem(operation);
                    break;
                case "move":
                    this.moveItem(operation.key, operation.beforeKey);
                    break;
                case "remove":
                    await this.removeItem(operation.key);
                    break;
                case "clear":
                    await this.clearItems();
                    break;
                case "reset":
                    await this.resetItems(operation.items ?? []);
                    break;
                default:
                    throw new Error(`Unsupported collection operation '${operation.kind}'.`);
            }
        }

        this.updateEmptyState();
    }

    private async resetItems(items: ReactiveCollectionJsItem[]): Promise<void> {
        await this.clearItems();

        if (!items || items.length === 0) {
            return;
        }

        const fragment = document.createDocumentFragment();
        const pendingRoots: Array<Promise<void>> = [];

        for (const item of items) {
            const hostElement = this.createHostElement(item);
            fragment.appendChild(hostElement);
            pendingRoots.push(this.mountItem(item, hostElement));
        }

        this.container.appendChild(fragment);
        await Promise.all(pendingRoots);
    }

    private async addItem(operation: ReactiveCollectionJsOperation): Promise<void> {
        if (!operation.key || !operation.registrationId || operation.registrationVersion === undefined || !operation.tagName) {
            throw new Error("Add operation is missing required fields.");
        }

        const item: ReactiveCollectionJsItem = {
            key: operation.key,
            registrationId: operation.registrationId,
            registrationVersion: operation.registrationVersion,
            tagName: operation.tagName,
            className: operation.className,
            style: operation.style
        };

        const hostElement = this.createHostElement(item);
        this.insertHostElement(hostElement, operation.beforeKey);
        await this.mountItem(item, hostElement);
    }

    private updateItem(operation: ReactiveCollectionJsOperation): void {
        if (!operation.key) {
            throw new Error("Update operation is missing a key.");
        }

        const existingHandle = this.itemsByKey.get(operation.key);
        if (!existingHandle) {
            return;
        }

        this.applyHostState(existingHandle.hostElement, operation.className, operation.style);

        if (operation.registrationId && operation.registrationVersion !== undefined) {
            existingHandle.rootComponent.setParameters({
                registrationId: operation.registrationId,
                version: operation.registrationVersion
            });
        }
    }

    private moveItem(key?: string | null, beforeKey?: string | null): void {
        if (!key) {
            throw new Error("Move operation is missing a key.");
        }

        const existingHandle = this.itemsByKey.get(key);
        if (!existingHandle) {
            return;
        }

        const anchorHandle = beforeKey ? this.itemsByKey.get(beforeKey) : undefined;
        const anchorElement = anchorHandle?.hostElement ?? null;

        if (anchorElement === existingHandle.hostElement) {
            return;
        }

        // Move is intentionally DOM-only: the existing host element and mounted root component
        // are preserved so callers can reorder hot collections without recreating Blazor islands.
        this.container.insertBefore(existingHandle.hostElement, anchorElement);
    }

    private async removeItem(key?: string | null): Promise<void> {
        if (!key) {
            throw new Error("Remove operation is missing a key.");
        }

        const existingHandle = this.itemsByKey.get(key);
        if (!existingHandle) {
            return;
        }

        this.itemsByKey.delete(key);
        await existingHandle.rootComponent.dispose();
        existingHandle.hostElement.remove();
    }

    private async clearItems(): Promise<void> {
        const handles = Array.from(this.itemsByKey.values());
        this.itemsByKey.clear();

        for (const handle of handles) {
            await handle.rootComponent.dispose();
            handle.hostElement.remove();
        }
    }

    private updateEmptyState(): void {
        if (!this.emptyStateElement) {
            return;
        }

        const hasItems = this.itemsByKey.size > 0;
        this.container.hidden = !hasItems;
        this.emptyStateElement.hidden = hasItems;
    }

    private createHostElement(item: ReactiveCollectionJsItem): HTMLElement {
        const tagName = item.tagName?.trim() || "div";
        const hostElement = document.createElement(tagName);
        hostElement.dataset["reactiveCollectionKey"] = item.key;
        this.applyHostState(hostElement, item.className, item.style);
        return hostElement;
    }

    private applyHostState(hostElement: HTMLElement, className?: string | null, style?: string | null): void {
        hostElement.className = [BASE_ITEM_CLASS, className ?? ""].filter(Boolean).join(" ");

        if (style && style.length > 0) {
            hostElement.setAttribute("style", style);
        } else {
            hostElement.removeAttribute("style");
        }
    }

    private insertHostElement(hostElement: HTMLElement, beforeKey?: string | null): void {
        const anchorHandle = beforeKey ? this.itemsByKey.get(beforeKey) : undefined;
        this.container.insertBefore(hostElement, anchorHandle?.hostElement ?? null);
    }

    private async mountItem(item: ReactiveCollectionJsItem, hostElement: HTMLElement): Promise<void> {
        const rootComponent = await BlazorFacade.rootComponents.add(hostElement, this.componentIdentifier, {
            registrationId: item.registrationId,
            version: item.registrationVersion
        });

        this.itemsByKey.set(item.key, {
            hostElement,
            rootComponent,
            tagName: item.tagName
        });
    }
}

export function createSession(
    container: HTMLElement,
    emptyStateElement: HTMLElement | null,
    componentIdentifier: string
): ReactiveCollectionSession {
    if (!container) {
        throw new Error("Container element must be specified.");
    }

    if (!componentIdentifier) {
        throw new Error("Component identifier must be specified.");
    }

    return new ReactiveCollectionSession(container, emptyStateElement, componentIdentifier);
}
