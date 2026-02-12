/**
 * Native HTML <dialog> helpers (showModal / close / close event).
 * Used by SettingsModal and AppLogsFiltersModal. Loaded as ES module for IJSObjectReference disposal.
 */

export function initDialog(dialogElement, dotNetRef) {
  if (!dialogElement) return;
  dialogElement.addEventListener('close', function handler() {
    // Ignore errors on page refresh/unload when the Blazor circuit is already gone
    dotNetRef.invokeMethodAsync('OnDialogClosed', dialogElement.returnValue ?? '').catch(() => {});
  });
}

export function showModal(dialogElement) {
  if (dialogElement && !dialogElement.open) {
    dialogElement.showModal();
  }
}

export function closeModal(dialogElement) {
  if (dialogElement && dialogElement.open) {
    dialogElement.close();
  }
}
