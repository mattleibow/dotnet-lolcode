const handlers = new WeakMap();

export function initialize(dialog, sampleList) {
    const handleKeyDown = event => {
        const option = event.target.closest("[role='option']");
        if (!option || !["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
            return;
        }

        const options = [...sampleList.querySelectorAll("[role='option']")];
        const currentIndex = options.indexOf(option);
        let nextIndex = currentIndex;

        if (event.key === "ArrowDown") {
            nextIndex = Math.min(options.length - 1, currentIndex + 1);
        } else if (event.key === "ArrowUp") {
            nextIndex = Math.max(0, currentIndex - 1);
        } else if (event.key === "Home") {
            nextIndex = 0;
        } else if (event.key === "End") {
            nextIndex = options.length - 1;
        }

        event.preventDefault();
        options[nextIndex]?.focus();
        options[nextIndex]?.click();
    };

    sampleList.addEventListener("keydown", handleKeyDown);
    handlers.set(dialog, { sampleList, handleKeyDown });
}

export function open(dialog, initialFocus) {
    if (!dialog.open) {
        dialog.showModal();
    }

    initialFocus.focus();
    initialFocus.select();
}

export function close(dialog, trigger) {
    if (dialog.open) {
        dialog.close();
    }

    trigger.focus();
}

export function dispose(dialog) {
    const handler = handlers.get(dialog);
    if (!handler) {
        return;
    }

    handler.sampleList.removeEventListener("keydown", handler.handleKeyDown);
    handlers.delete(dialog);
}
