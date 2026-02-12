/**
 * Positioning and scroll-close for config option help popover.
 * Called from ConfigOptionHelpModal.razor via IJSRuntime.InvokeAsync.
 */
window.getConfigOptionHelpAnchorRect = function (anchorId) {
  var el = document.getElementById(anchorId);
  if (!el) return null;
  var r = el.getBoundingClientRect();
  return { top: r.top, left: r.left, bottom: r.bottom, width: r.width, height: r.height };
};

var _configOptionHelpScrollHandler = null;

window.addConfigOptionHelpScrollCloseListener = function (dotNetRef) {
  if (_configOptionHelpScrollHandler) return;
  _configOptionHelpScrollHandler = function () {
    dotNetRef.invokeMethodAsync('OnScrollClose').catch(function () {});
  };
  window.addEventListener('scroll', _configOptionHelpScrollHandler, true);
};

window.removeConfigOptionHelpScrollCloseListener = function () {
  if (!_configOptionHelpScrollHandler) return;
  window.removeEventListener('scroll', _configOptionHelpScrollHandler, true);
  _configOptionHelpScrollHandler = null;
};
