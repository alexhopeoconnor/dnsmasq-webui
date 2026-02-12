/**
 * Positioning and scroll-close for config option help popover.
 * Loaded as ES module by ConfigOptionHelpModal for IJSObjectReference disposal.
 */

export function getAnchorRect(anchorId) {
  const el = document.getElementById(anchorId);
  if (!el) return null;
  const r = el.getBoundingClientRect();
  return { top: r.top, left: r.left, bottom: r.bottom, width: r.width, height: r.height };
}

let _scrollHandler = null;

export function addScrollCloseListener(dotNetRef) {
  if (_scrollHandler) return;
  _scrollHandler = function () {
    dotNetRef.invokeMethodAsync('OnScrollClose').catch(function () {});
  };
  window.addEventListener('scroll', _scrollHandler, true);
}

export function removeScrollCloseListener() {
  if (!_scrollHandler) return;
  window.removeEventListener('scroll', _scrollHandler, true);
  _scrollHandler = null;
}
