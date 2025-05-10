console.log("Initializing BT scripts");

try {
    document.addEventListener('keydown', handleKeyDown, true);
} catch (error) {
    console.error("BT Script initialization failed:", error);
}

function handleKeyDown(e: KeyboardEvent): void {
    try {
        const target = e.target as HTMLElement;
        const tag = target?.tagName?.toLowerCase() || '';
        const isEditable = target?.isContentEditable ||
            tag === 'input' ||
            tag === 'textarea';

        if (!isEditable && e.ctrlKey && e.key.toLowerCase() === 'a') {
            console.log("Blocked Ctrl+A outside input at", new Date().toISOString());
            e.preventDefault();
        }
    } catch (err) {
        console.error("Error handling keydown event in BT script:", err);
    }
}