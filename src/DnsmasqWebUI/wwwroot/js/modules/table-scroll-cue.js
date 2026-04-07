// Generic table scroll cues for horizontally scrollable tables.
// Works with reusable data attributes:
//   host:   [data-table-scroll-cue]
//   region: [data-table-scroll-region]

const OUTER_SELECTOR = '[data-table-scroll-cue]';
const WRAPPER_SELECTOR = '[data-table-scroll-region]';
const POLL_MS = 100;

const BTN_LEFT_CLASS = 'app-table-cue-btn--left';
const BTN_RIGHT_CLASS = 'app-table-cue-btn--right';
const CUE_BTN_CLASS = 'app-table-cue-btn';
const RECENT_CUE_INTERACTION_MS = 300;

let pollId = null;
let resizeListener = null;
const scrollListeners = new WeakMap();
let lastCueInteractionAt = 0;

function markCueInteraction() {
    lastCueInteractionAt = Date.now();
}

function applyScrollCue(scrollEl, hostEl) {
    if (!scrollEl || !hostEl) return;

    const scrollWidth = scrollEl.scrollWidth;
    const clientWidth = scrollEl.clientWidth;
    const scrollLeft = scrollEl.scrollLeft;
    const scrollable = scrollWidth > clientWidth;
    const atEnd = scrollable && scrollLeft >= scrollWidth - clientWidth - 1;
    const atStart = scrollLeft <= 0;
    const canScrollRight = scrollable && !atEnd;
    const canScrollLeft = !atStart;

    hostEl.classList.toggle('can-scroll-right', canScrollRight);
    hostEl.classList.toggle('at-end', atEnd);
    hostEl.classList.toggle('at-start', atStart);
    hostEl.classList.toggle('can-scroll-left', canScrollLeft);
}

function scrollToSide(scrollEl, side) {
    if (!scrollEl) return;

    const maxScroll = scrollEl.scrollWidth - scrollEl.clientWidth;
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const behavior = reducedMotion ? 'auto' : 'smooth';

    scrollEl.scrollTo({
        left: side === 'left' ? 0 : maxScroll,
        behavior
    });
}

function ensureCueButtons(hostEl) {
    const scrollEl = hostEl.querySelector(WRAPPER_SELECTOR);
    if (!scrollEl) return;

    let leftBtn = hostEl.querySelector('.' + BTN_LEFT_CLASS);
    let rightBtn = hostEl.querySelector('.' + BTN_RIGHT_CLASS);

    if (!leftBtn) {
        leftBtn = document.createElement('button');
        leftBtn.type = 'button';
        leftBtn.className = CUE_BTN_CLASS + ' ' + BTN_LEFT_CLASS;
        leftBtn.setAttribute('aria-label', 'Scroll table left');
        leftBtn.addEventListener('pointerdown', markCueInteraction);
        leftBtn.addEventListener('click', function () {
            markCueInteraction();
            scrollToSide(hostEl.querySelector(WRAPPER_SELECTOR), 'left');
        });
        hostEl.appendChild(leftBtn);
    }

    if (!rightBtn) {
        rightBtn = document.createElement('button');
        rightBtn.type = 'button';
        rightBtn.className = CUE_BTN_CLASS + ' ' + BTN_RIGHT_CLASS;
        rightBtn.setAttribute('aria-label', 'Scroll table right');
        rightBtn.addEventListener('pointerdown', markCueInteraction);
        rightBtn.addEventListener('click', function () {
            markCueInteraction();
            scrollToSide(hostEl.querySelector(WRAPPER_SELECTOR), 'right');
        });
        hostEl.appendChild(rightBtn);
    }

    if (!scrollListeners.has(hostEl)) {
        const onScroll = function () {
            applyScrollCue(scrollEl, hostEl);
        };
        scrollEl.addEventListener('scroll', onScroll);
        scrollListeners.set(hostEl, { scroll: onScroll });
    }
}

function pollScrollCue() {
    const outers = document.querySelectorAll(OUTER_SELECTOR);
    outers.forEach(function (hostEl) {
        const scrollEl = hostEl.querySelector(WRAPPER_SELECTOR);
        if (scrollEl) {
            ensureCueButtons(hostEl);
            applyScrollCue(scrollEl, hostEl);
        }
    });
}

function startPolling() {
    if (pollId != null) return;
    pollScrollCue();
    pollId = setInterval(pollScrollCue, POLL_MS);
}

function stopPolling() {
    if (pollId != null) {
        clearInterval(pollId);
        pollId = null;
    }
}

function setupListeners() {
    if (resizeListener != null) return;
    resizeListener = pollScrollCue;
    window.addEventListener('resize', resizeListener);
}

function removeHostListeners(hostEl) {
    const scrollEl = hostEl.querySelector(WRAPPER_SELECTOR);
    const entry = scrollListeners.get(hostEl);
    if (entry) {
        if (scrollEl) {
            scrollEl.removeEventListener('scroll', entry.scroll);
        }
        scrollListeners.delete(hostEl);
    }
}

export function initTableScrollCue() {
    disposeTableScrollCue();
    startPolling();
    setupListeners();
}

export function updateTableScrollCue() {
    pollScrollCue();
}

export function clearTableSelection() {
    try {
        window.getSelection()?.removeAllRanges();
    } catch (_) {
    }
}

export function disposeTableScrollCue() {
    stopPolling();

    if (resizeListener) {
        window.removeEventListener('resize', resizeListener);
        resizeListener = null;
    }

    document.querySelectorAll(OUTER_SELECTOR).forEach(function (hostEl) {
        removeHostListeners(hostEl);
        const left = hostEl.querySelector('.' + BTN_LEFT_CLASS);
        const right = hostEl.querySelector('.' + BTN_RIGHT_CLASS);
        if (left) left.remove();
        if (right) right.remove();
    });
}

/**
 * Returns true if the active element is not inside `container` (e.g. focus left a table row).
 * Use after a short delay following focusout so document.activeElement has updated.
 * @param {HTMLElement | null} container
 */
export function isFocusOutsideElement(container) {
    if (!container) return true;
    const active = document.activeElement;
    if (!active) return true;
    return !container.contains(active);
}

export function wasRecentCueInteraction() {
    return Date.now() - lastCueInteractionAt <= RECENT_CUE_INTERACTION_MS;
}
