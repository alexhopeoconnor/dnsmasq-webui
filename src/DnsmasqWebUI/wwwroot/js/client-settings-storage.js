/**
 * Local storage access for client settings. Used by IClientSettingsService.
 * Loaded as ES module for IJSObjectReference disposal.
 */
const STORAGE_KEY = 'dnsmasq-webui.clientSettings';

export function getItem() {
  try {
    return window.localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

export function setItem(value) {
  try {
    window.localStorage.setItem(STORAGE_KEY, value);
  } catch (e) {
    console.warn('Failed to save client settings:', e);
  }
}
