/**
 * Touch / coarse-pointer devices: show Effective names help as a toast instead of relying on hover.
 */
export function preferEffectiveNamesHelpToast() {
  try {
    if (window.matchMedia("(pointer: coarse)").matches) return true;
    if (window.matchMedia("(hover: none)").matches) return true;
    return false;
  } catch {
    return false;
  }
}
