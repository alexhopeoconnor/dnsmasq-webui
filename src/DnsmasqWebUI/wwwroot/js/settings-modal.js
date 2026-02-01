/**
 * Native HTML <dialog> helpers for the client settings modal.
 * Uses HTMLDialogElement.showModal() / close() — no Bootstrap or other framework.
 * Loaded as ES module for IJSObjectReference disposal.
 */

export function initDialog(dialogElement, dotNetRef) {
  if (!dialogElement) return;
  dialogElement.addEventListener('close', function handler() {
    dotNetRef.invokeMethodAsync('OnDialogClosed', dialogElement.returnValue ?? '');
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
