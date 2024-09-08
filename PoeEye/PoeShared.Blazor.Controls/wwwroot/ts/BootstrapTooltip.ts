import { Tooltip } from 'bootstrap';
export function setup(id: string, options: string): void {
    try {
        destroy(id);
        const identifier = `bootstrap-tooltip-${id}`;
        const element = document.getElementById(identifier);

        if (element) {
            const oOptions = JSON.parse(options);
            const tooltip = Tooltip.getOrCreateInstance(element, oOptions);
        } else {
            console.error(`Element with ID ${identifier} not found.`);
        }
    } catch (e) {
        console.error(`Error from Tooltip Setup: ${(e as Error).message}`);
        console.error(`Parameters = ID: ${id} | Options: ${options}`);
        console.error(e);
    }
}

export function destroy(id: string): void {
    try {
        const identifier = `bootstrap-tooltip-${id}`;
        const element = document.getElementById(identifier);

        if (element) {
            const tooltip = Tooltip.getInstance(element);

            if (tooltip) {
                tooltip.dispose();
            }
        } else {
            //most probably the element has already been removed thus we do not really need to do anything
            //FIXME Potential memory leak? 
        }
    } catch (e) {
        console.error(`Error from Tooltip Destroy: ${(e as Error).message}`);
        console.error(`Parameters = ID: ${id}`);
        console.error(e);
    }
}

export function update(id: string, options: string): void {
    try {
        setup(id, options);
    } catch (e) {
        console.error(`Error from Tooltip Update: ${(e as Error).message}`);
        console.error(`Parameters = ID: ${id} | Options: ${options}`);
        console.error(e);
    }
}
