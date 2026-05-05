import {shouldSuppressBrowserShortcut, suppressWellKnownBrowserShortcuts} from '../wwwroot/ts/blazorUtils.input';

type TestKeyboardEvent = Parameters<typeof shouldSuppressBrowserShortcut>[0];

function keyEvent(key: string, event: Partial<TestKeyboardEvent> = {}): TestKeyboardEvent {
    return {
        key,
        ctrlKey: false,
        shiftKey: false,
        altKey: false,
        metaKey: false,
        defaultPrevented: false,
        ...event
    };
}

describe('browser shortcut suppression', () => {
    it.each([
        keyEvent('a', {ctrlKey: true}),
        keyEvent('p', {ctrlKey: true}),
        keyEvent('f', {ctrlKey: true}),
        keyEvent('s', {ctrlKey: true}),
        keyEvent('o', {ctrlKey: true}),
        keyEvent('r', {ctrlKey: true}),
        keyEvent('r', {ctrlKey: true, shiftKey: true}),
        keyEvent('F5')
    ])('suppresses default WebView shortcut %s', event => {
        expect(shouldSuppressBrowserShortcut(event)).toBe(true);
    });

    it.each([
        keyEvent('c', {ctrlKey: true}),
        keyEvent('v', {ctrlKey: true}),
        keyEvent('x', {ctrlKey: true})
    ])('does not suppress clipboard shortcut %s', event => {
        expect(shouldSuppressBrowserShortcut(event)).toBe(false);
    });

    it.each([
        {tagName: 'input'},
        {tagName: 'textarea'},
        {tagName: 'select'},
        {tagName: 'span', isContentEditable: true}
    ])('allows Ctrl+A in editable target %s', target => {
        expect(shouldSuppressBrowserShortcut(keyEvent('a', {ctrlKey: true, target}))).toBe(false);
    });

    it('does not treat contenteditable false as editable', () => {
        const target = {
            tagName: 'span',
            isContentEditable: false,
            closest: (selector: string) => selector === '[contenteditable]' ? {isContentEditable: false} : null
        };

        expect(shouldSuppressBrowserShortcut(keyEvent('a', {ctrlKey: true, target}))).toBe(true);
    });

    it('skips target inside explicit allow subtree', () => {
        const target = {
            closest: (selector: string) => selector === '[data-poe-browser-shortcuts="allow"]' ? {} : null
        };

        expect(shouldSuppressBrowserShortcut(keyEvent('p', {ctrlKey: true, target}))).toBe(false);
    });

    it('uses custom shortcut list when provided', () => {
        const options = {
            shortcuts: [{key: 'k', ctrlKey: true}]
        };

        expect(shouldSuppressBrowserShortcut(keyEvent('k', {ctrlKey: true}), options)).toBe(true);
        expect(shouldSuppressBrowserShortcut(keyEvent('p', {ctrlKey: true}), options)).toBe(false);
    });

    it('respects already prevented events', () => {
        expect(shouldSuppressBrowserShortcut(keyEvent('p', {ctrlKey: true, defaultPrevented: true}))).toBe(false);
    });

    it('uses composed path for shadow-dom editable targets and allow markers', () => {
        const shadowInput = {tagName: 'input'};
        const shadowAllowedChild = {
            closest: (selector: string) => selector === '[data-poe-browser-shortcuts="allow"]' ? {} : null
        };
        const host = {tagName: 'div'};

        expect(shouldSuppressBrowserShortcut(keyEvent('a', {
            ctrlKey: true,
            target: host,
            composedPath: () => [shadowInput, host] as unknown as EventTarget[]
        }))).toBe(false);

        expect(shouldSuppressBrowserShortcut(keyEvent('p', {
            ctrlKey: true,
            target: host,
            composedPath: () => [shadowAllowedChild, host] as unknown as EventTarget[]
        }))).toBe(false);
    });

    it('installs a prevent-default-only listener and disposes it', () => {
        const originalDocument = globalThis.document;
        const addEventListener = jest.fn();
        const removeEventListener = jest.fn();
        const fakeDocument = {addEventListener, removeEventListener};
        Object.defineProperty(globalThis, 'document', {
            configurable: true,
            value: fakeDocument
        });

        try {
            const handle = suppressWellKnownBrowserShortcuts();
            const [, listener, useCapture] = addEventListener.mock.calls[0];
            const event = {
                ...keyEvent('p', {ctrlKey: true}),
                preventDefault: jest.fn(),
                stopPropagation: jest.fn()
            };

            listener(event);

            expect(addEventListener).toHaveBeenCalledWith('keydown', listener, true);
            expect(useCapture).toBe(true);
            expect(event.preventDefault).toHaveBeenCalledTimes(1);
            expect(event.stopPropagation).not.toHaveBeenCalled();

            handle.dispose();

            expect(removeEventListener).toHaveBeenCalledWith('keydown', listener, true);
        } finally {
            Object.defineProperty(globalThis, 'document', {
                configurable: true,
                value: originalDocument
            });
        }
    });
});
