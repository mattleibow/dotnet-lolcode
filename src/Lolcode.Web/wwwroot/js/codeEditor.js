const scrollHandlers = new WeakMap();

export function initialize(editor, gutter) {
    const syncScroll = () => {
        gutter.scrollTop = editor.scrollTop;
    };

    scrollHandlers.set(editor, syncScroll);
    editor.addEventListener("scroll", syncScroll, { passive: true });
    syncScroll();
}

export function dispose(editor) {
    const syncScroll = scrollHandlers.get(editor);
    if (!syncScroll) {
        return;
    }

    editor.removeEventListener("scroll", syncScroll);
    scrollHandlers.delete(editor);
}
