/**
 * Logs terminal: append/replace content with optional max lines and auto-scroll. Used by LogsSection for dnsmasq and app logs.
 * @param {string} elementId - id of the pre element
 * @param {string} content - log content to append or replace
 * @param {{ maxLines?: number, autoScroll?: boolean }} [options] - maxLines: truncate to last N lines; autoScroll: scroll to bottom (default true)
 */
export function appendLogs(elementId, content, options = {}) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.textContent += content;
    truncateToMaxLines(el, options.maxLines);
    if (options.autoScroll !== false)
        scrollLogsToBottom(elementId);
}

export function replaceLogs(elementId, content, options = {}) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.textContent = content;
    truncateToMaxLines(el, options.maxLines);
    if (options.autoScroll !== false)
        scrollLogsToBottom(elementId);
}

function truncateToMaxLines(el, maxLines) {
    if (maxLines == null || maxLines <= 0) return;
    const text = el.textContent || '';
    const lines = text.split('\n');
    if (lines.length <= maxLines) return;
    el.textContent = lines.slice(-maxLines).join('\n');
}

export function scrollLogsToBottom(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.scrollTop = el.scrollHeight;
}
